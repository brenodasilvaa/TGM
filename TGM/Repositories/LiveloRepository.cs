using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using TGM.Models.Livelo;

namespace TGM.Repositories
{
    internal class LiveloRepository : ILiveloRepository
    {
        const string UrlInfoPartners = "https://www.livelo.com.br/ccstore/v1/files/thirdparty/config_partners_compre_e_pontue.json";
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
            var infoPartnersResult = await _httpClient.GetAsync(UrlPartnerParityByCode + partnerCode, cancellation);

            var infoPartners = await infoPartnersResult.Content.ReadAsStringAsync();

            var partnersParities = JsonSerializer.Deserialize<List<PartnerParityModel>>(infoPartners,
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            return partnersParities.Any() ? partnersParities[0].LegalTerms : string.Empty;
        }

        public async Task<Partner?> GetPartners(CancellationToken cancellation)
        {
            var infoPartnersResult = await _httpClient.GetAsync(UrlInfoPartners, cancellation);

            var infoPartners = await infoPartnersResult.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Partner>(infoPartners, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});
        }

        public async Task<List<PartnerParityModel>?> GetPartnersParities(CancellationToken cancellation)
        {
            var infoPartnersResult = await _httpClient.GetAsync(UrlPartnerParity);

            var infoPartners = await infoPartnersResult.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<PartnerParityModel>>(infoPartners, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }
    }
}
