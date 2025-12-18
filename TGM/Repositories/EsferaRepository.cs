using System.Net;
using System.Text.Json;
using TGM.Models.Esfera;

namespace TGM.Repositories
{
    internal class EsferaRepository : IEsferaRepository
    {
        const string UrlPartnerParity = "https://apigw.esfera.com.vc/bff-product/ehcs/products?categoryId=esf02163";
        private readonly HttpClient _httpClient;

        public EsferaRepository()
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

        public async Task<List<ParityInfoModel>?> GetPartnersParities(CancellationToken cancellation)
        {
            var infoPartnersResult = await _httpClient.GetAsync(UrlPartnerParity);

            var infoPartners = await infoPartnersResult.Content.ReadAsStringAsync();

            var teste =  JsonSerializer.Deserialize<Parities>(infoPartners, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return teste.Items;
        }
    }
}
