using Amazon.S3;
using Amazon.S3.Model;
using System.Text;
using TgmCore.Models;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public class CsvGenerationService(IAmazonS3 s3Client, string bucketName) : ICsvGenerationService
{
    public Task<string> GenerateCsvAsync(IEnumerable<RetornoParity> parityResults, string fileName)
        => throw new NotImplementedException("Use GenerateCsvWithProgramAsync to include program information");

    public async Task<string> GenerateCsvWithProgramAsync(IEnumerable<ParityResultWithProgram> parityResults)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

        await writer.WriteLineAsync("Programa;Parceiro;Bonificação;Validade;Termos Legais");

        foreach (var item in parityResults)
            await writer.WriteLineAsync($"{item.Program};{item.Result.Nome};{item.Result.Pontuacao};{item.Result.Validade};{item.Result.LegalTerms ?? string.Empty}");

        await writer.FlushAsync();
        memoryStream.Position = 0;

        var objectKey = $"partners-promo-{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}.csv";

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = memoryStream,
            ContentType = "text/csv",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        });

        return objectKey;
    }
}
