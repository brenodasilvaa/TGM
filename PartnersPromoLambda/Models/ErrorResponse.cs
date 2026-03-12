using System.Text.Json.Serialization;

namespace PartnersPromoLambda.Models;

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
