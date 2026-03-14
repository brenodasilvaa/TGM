using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;
using PartnersPromoLambda.Models;
using PartnersPromoLambda.Services;
using Amazon.S3;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using TgmCore;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace PartnersPromoLambda.Handlers;

public class PartnersPromoHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ValidationService _validationService;
    private readonly IExecutionLimitService _executionLimitService;
    private readonly CsvGenerationService _csvService;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly string _bucketName;

    public PartnersPromoHandler()
    {
        var verificationCode = Environment.GetEnvironmentVariable("VERIFICATION_CODE")
            ?? throw new InvalidOperationException("VERIFICATION_CODE environment variable is required");

        var dailyLimit = int.Parse(Environment.GetEnvironmentVariable("DAILY_EXECUTION_LIMIT") ?? "4");

        _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME")
            ?? throw new InvalidOperationException("S3_BUCKET_NAME environment variable is required");

        var dynamoDbTableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME")
            ?? throw new InvalidOperationException("DYNAMODB_TABLE_NAME environment variable is required");

        var services = new ServiceCollection();

        services.AddSingleton<IAmazonS3>(new AmazonS3Client());
        services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient());

        services.AddTgmCore();

        services.AddSingleton(new ValidationService(verificationCode));
        services.AddSingleton<IExecutionLimitService>(sp =>
            new ExecutionLimitService(sp.GetRequiredService<IAmazonDynamoDB>(), dynamoDbTableName, dailyLimit));
        services.AddScoped<IPartnersPromoProcessingOrchestrator, PartnersPromoProcessingOrchestrator>();
        services.AddSingleton(sp => new CsvGenerationService(sp.GetRequiredService<IAmazonS3>(), _bucketName));
        services.AddSingleton<IPresignedUrlService, PresignedUrlService>();

        _serviceProvider = services.BuildServiceProvider();

        _validationService = _serviceProvider.GetRequiredService<ValidationService>();
        _executionLimitService = _serviceProvider.GetRequiredService<IExecutionLimitService>();
        _csvService = _serviceProvider.GetRequiredService<CsvGenerationService>();
        _presignedUrlService = _serviceProvider.GetRequiredService<IPresignedUrlService>();
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Request received at {DateTime.UtcNow:o}");

        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return CreateErrorResponse(400, "BadRequest", "Request body is required");

            PartnersPromoRequest? promoRequest;
            try
            {
                promoRequest = JsonSerializer.Deserialize<PartnersPromoRequest>(request.Body);
                if (promoRequest == null)
                    return CreateErrorResponse(400, "BadRequest", "Invalid request format");
            }
            catch (JsonException)
            {
                return CreateErrorResponse(400, "BadRequest", "Invalid JSON format");
            }

            if (!_validationService.ValidateVerificationCode(promoRequest.VerificationCode))
                return CreateErrorResponse(401, "Unauthorized", "Invalid verification code");

            var (isValid, errorMessage) = _validationService.ValidateMinimumScore(promoRequest.MinimumScore);
            if (!isValid)
                return CreateErrorResponse(400, "BadRequest", errorMessage!);

            if (!await _executionLimitService.CanExecuteAsync())
                return CreateErrorResponse(429, "TooManyRequests", "Daily execution limit reached. Please try again tomorrow.");

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IPartnersPromoProcessingOrchestrator>();

            var results = await orchestrator.ProcessAllParitiesWithProgramAsync(
                promoRequest.MinimumScore,
                context.RemainingTime > TimeSpan.FromSeconds(30)
                    ? new CancellationTokenSource(context.RemainingTime.Subtract(TimeSpan.FromSeconds(30))).Token
                    : CancellationToken.None);

            var objectKey = await _csvService.GenerateCsvWithProgramAsync(results);
            var presignedUrl = await _presignedUrlService.GeneratePresignedUrlAsync(_bucketName, objectKey, TimeSpan.FromHours(1));

            await _executionLimitService.IncrementExecutionCountAsync();

            var response = new PartnersPromoResponse
            {
                DownloadUrl = presignedUrl,
                FileName = objectKey,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}\n{ex.StackTrace}");
            return CreateErrorResponse(500, "InternalServerError", "An error occurred while processing your request");
        }
    }

    private static APIGatewayProxyResponse CreateErrorResponse(int statusCode, string error, string message) =>
        new()
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(new ErrorResponse { Error = error, Message = message }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
}
