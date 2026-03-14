namespace PartnersPromoLambda.Services;

public interface IPresignedUrlService
{
    Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, TimeSpan expiration);
}
