using System.Text.Json.Serialization;

namespace PartnersPromoLambda.Models;

public class PartnersPromoRequest
{
    [JsonPropertyName("minimumScore")]
    public int MinimumScore { get; set; }

    [JsonPropertyName("verificationCode")]
    public string VerificationCode { get; set; } = string.Empty;
}
