using Amazon.S3;
using Amazon.S3.Model;
using System.Text;
using TGM.Models;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public class CsvGenerationService : ICsvGenerationService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public CsvGenerationService(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public async Task<string> GenerateCsvAsync(IEnumerable<RetornoParity> parityResults, string fileName)
    {
        // This method is kept for interface compatibility but not recommended
        // Use GenerateCsvWithProgramAsync instead
        throw new NotImplementedException("Use GenerateCsvWithProgramAsync to include program information");
    }

    public async Task<string> GenerateCsvWithProgramAsync(IEnumerable<ParityResultWithProgram> parityResults)
    {
        // Generate CSV content in memory
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

        // Write header
        await writer.WriteLineAsync("Programa;Parceiro;Bonificação;Validade;Termos Legais");

        // Write data rows
        foreach (var item in parityResults)
        {
            var line = $"{item.Program};{item.Result.Nome};{item.Result.Pontuacao};{item.Result.Validade};{item.Result.LegalTerms ?? string.Empty}";
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
        memoryStream.Position = 0;

        // Generate timestamp for filename
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
        var objectKey = $"partners-promo-{timestamp}.csv";

        // Upload to S3
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = memoryStream,
            ContentType = "text/csv",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(putRequest);

        return objectKey;
    }
}
