using Amazon.S3;
using Amazon.S3.Model;

namespace PartnersPromoLambda.Services;

public class PresignedUrlService : IPresignedUrlService
{
    private readonly IAmazonS3 _s3Client;

    public PresignedUrlService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, TimeSpan expiration)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiration),
            Verb = HttpVerb.GET
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }
}
