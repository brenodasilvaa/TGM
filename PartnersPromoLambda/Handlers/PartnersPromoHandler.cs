using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;
using PartnersPromoLambda.Models;
using PartnersPromoLambda.Services;
using Amazon.S3;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using TGM.Services.Interfaces;
using TGM.Services;
using TGM.Services.Livelo;
using TGM.Services.Esfera;
using TGM.Repositories;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace PartnersPromoLambda.Handlers;

public class PartnersPromoHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ValidationService _validationService;
    private readonly IExecutionLimitService _executionLimitService;
    private readonly IPartnersPromoProcessingOrchestrator _partnersPromoOrchestrator;
    private readonly CsvGenerationService _csvService;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly string _bucketName;

    public PartnersPromoHandler()
    {
        // Read environment variables
        var verificationCode = Environment.GetEnvironmentVariable("VERIFICATION_CODE") 
            ?? throw new InvalidOperationException("VERIFICATION_CODE environment variable is required");
        
        var dailyLimitStr = Environment.GetEnvironmentVariable("DAILY_EXECUTION_LIMIT") ?? "4";
        var dailyLimit = int.Parse(dailyLimitStr);
        
        _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") 
            ?? throw new InvalidOperationException("S3_BUCKET_NAME environment variable is required");
        
        var dynamoDbTableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME") 
            ?? throw new InvalidOperationException("DYNAMODB_TABLE_NAME environment variable is required");

        // Setup dependency injection
        var services = new ServiceCollection();
        
        // AWS Services
        services.AddSingleton<IAmazonS3>(new AmazonS3Client());
        services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient());
        
        // TGM Services
        services.AddScoped<ILiveloRepository, LiveloRepository>();
        services.AddScoped<IEsferaRepository, EsferaRepository>();
        services.AddScoped<LiveloParityService>();
        services.AddScoped<EsferaParityService>();
        services.AddScoped<IParityServiceFactory, ParityServiceFactory>();
        
        // Lambda Services
        services.AddSingleton(new ValidationService(verificationCode));
        services.AddSingleton<IExecutionLimitService>(sp => 
            new ExecutionLimitService(
                sp.GetRequiredService<IAmazonDynamoDB>(), 
                dynamoDbTableName, 
                dailyLimit));
        services.AddScoped<IPartnersPromoProcessingOrchestrator, PartnersPromoProcessingOrchestrator>();
        services.AddSingleton(sp => 
            new CsvGenerationService(
                sp.GetRequiredService<IAmazonS3>(), 
                _bucketName));
        services.AddSingleton<IPresignedUrlService, PresignedUrlService>();

        _serviceProvider = services.BuildServiceProvider();
        
        // Get singleton services
        _validationService = _serviceProvider.GetRequiredService<ValidationService>();
        _executionLimitService = _serviceProvider.GetRequiredService<IExecutionLimitService>();
        _csvService = _serviceProvider.GetRequiredService<CsvGenerationService>();
        _presignedUrlService = _serviceProvider.GetRequiredService<IPresignedUrlService>();
        
        // Orchestrator needs to be created per request (uses scoped services)
        _partnersPromoOrchestrator = null!; // Will be initialized per request
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(
        APIGatewayProxyRequest request, 
        ILambdaContext context)
    {
        context.Logger.LogInformation($"Request received at {DateTime.UtcNow:o}");

        try
        {
            // Parse request body
            if (string.IsNullOrEmpty(request.Body))
            {
                return CreateErrorResponse(400, "BadRequest", "Request body is required");
            }

            PartnersPromoRequest? promoRequest;
            try
            {
                promoRequest = JsonSerializer.Deserialize<PartnersPromoRequest>(request.Body);
                if (promoRequest == null)
                {
                    return CreateErrorResponse(400, "BadRequest", "Invalid request format");
                }
            }
            catch (JsonException)
            {
                return CreateErrorResponse(400, "BadRequest", "Invalid JSON format");
            }

            context.Logger.LogInformation($"Processing request with MinimumScore: {promoRequest.MinimumScore}");

            // Validate verification code
            if (!_validationService.ValidateVerificationCode(promoRequest.VerificationCode))
            {
                context.Logger.LogWarning("Authentication failed - invalid verification code");
                return CreateErrorResponse(401, "Unauthorized", "Invalid verification code");
            }

            // Validate minimum score
            var (isValid, errorMessage) = _validationService.ValidateMinimumScore(promoRequest.MinimumScore);
            if (!isValid)
            {
                context.Logger.LogWarning($"Validation failed: {errorMessage}");
                return CreateErrorResponse(400, "BadRequest", errorMessage!);
            }

            // Check execution limit
            if (!await _executionLimitService.CanExecuteAsync())
            {
                context.Logger.LogWarning("Daily execution limit reached");
                return CreateErrorResponse(429, "TooManyRequests", "Daily execution limit reached. Please try again tomorrow.");
            }

            // Create scoped orchestrator for this request
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IPartnersPromoProcessingOrchestrator>();

            // Process parities
            context.Logger.LogInformation("Starting parity processing");
            var results = await orchestrator.ProcessAllParitiesWithProgramAsync(
                promoRequest.MinimumScore, 
                context.RemainingTime > TimeSpan.FromSeconds(30) 
                    ? new CancellationTokenSource(context.RemainingTime.Subtract(TimeSpan.FromSeconds(30))).Token 
                    : CancellationToken.None);

            // Generate CSV and upload to S3
            context.Logger.LogInformation("Generating CSV file");
            var objectKey = await _csvService.GenerateCsvWithProgramAsync(results);

            // Generate presigned URL
            context.Logger.LogInformation("Generating presigned URL");
            var presignedUrl = await _presignedUrlService.GeneratePresignedUrlAsync(
                _bucketName, 
                objectKey, 
                TimeSpan.FromHours(1));

            // Increment execution counter
            await _executionLimitService.IncrementExecutionCountAsync();

            // Build response
            var response = new PartnersPromoResponse
            {
                DownloadUrl = presignedUrl,
                FileName = objectKey,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            context.Logger.LogInformation("Request completed successfully");

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            
            return CreateErrorResponse(500, "InternalServerError", "An error occurred while processing your request");
        }
    }

    private APIGatewayProxyResponse CreateErrorResponse(int statusCode, string error, string message)
    {
        var errorResponse = new ErrorResponse
        {
            Error = error,
            Message = message
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(errorResponse),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };
    }
}
