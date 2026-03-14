using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using TgmCore.Models.Livelo;

namespace TgmCore.Repositories;

public class LiveloRepository : ILiveloRepository
{
    const string UrlInfoPartners = "https://www.livelo.com.br/juntar-pontos/todos-os-parceiros";
    const string UrlPartnerParity = "https://apis.pontoslivelo.com.br/api-bff-partners-parities/v1/parities/active";
    const string UrlPartnerParityByCode = $"{UrlPartnerParity}?partnersCodes=";
    private readonly HttpClient _httpClient;

    public LiveloRepository()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        _httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        });

        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
    }

    public async Task<string?> GetPartnerLegalTerm(string partnerCode, CancellationToken cancellation)
    {
        var result = await _httpClient.GetAsync(UrlPartnerParityByCode + partnerCode, cancellation);
        var content = await result.Content.ReadAsStringAsync(cancellation);
        var parities = JsonSerializer.Deserialize<List<PartnerParityModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return parities?.Any() == true
            ? Regex.Replace(parities[0].LegalTerms, "<.*?>", string.Empty).Replace(";", string.Empty)
            : string.Empty;
    }

    public async Task<Partner?> GetPartners(CancellationToken cancellation)
    {
        var result = await _httpClient.GetAsync(UrlInfoPartners, cancellation);
        var content = await result.Content.ReadAsStringAsync(cancellation);
        var serialized = GetPartnersObjectSerialized(content);
        return JsonSerializer.Deserialize<Partner>(serialized, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<PartnerParityModel>?> GetPartnersParities(CancellationToken cancellation)
    {
        var result = await _httpClient.GetAsync(UrlPartnerParity, cancellation);
        var content = await result.Content.ReadAsStringAsync(cancellation);
        return JsonSerializer.Deserialize<List<PartnerParityModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static string GetPartnersObjectSerialized(string input)
    {
        string key = "\"configPartners\"";
        int startIndex = input.LastIndexOf(key);
        if (startIndex < 0) return string.Empty;

        int jsonStart = input.IndexOfAny(['{', '['], startIndex);
        if (jsonStart < 0) return string.Empty;

        char openChar = input[jsonStart];
        char closeChar = openChar == '{' ? '}' : ']';
        int balance = 0, i = jsonStart;

        for (; i < input.Length; i++)
        {
            if (input[i] == openChar) balance++;
            else if (input[i] == closeChar) balance--;
            if (balance == 0) break;
        }

        return balance == 0
            ? "{ \"configPartners\": " + input.Substring(jsonStart, i - jsonStart + 1) + " }"
            : string.Empty;
    }
}
