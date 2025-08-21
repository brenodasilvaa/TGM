using System.Text.RegularExpressions;
using TGM.Models.Livelo;
using TGM.Repositories;

namespace TGM.Services.Livelo
{
    internal class ParityService(ILiveloRepository liveloRepository) : IParityService
    {
        public async Task<List<RetornoParity>> GetParities(CancellationToken cancellation)
        {
            Console.WriteLine("Obtendo lista de parceiros...");
            var partnersInfo = await liveloRepository.GetPartners(cancellation);

            Console.WriteLine("Obtendo lista de promoções...");
            var partnersParities = await liveloRepository.GetPartnersParities(cancellation);

            var retorno = new List<RetornoParity>();

            foreach (var partner in partnersParities.Where(x => x.Promotion).OrderByDescending(x => x.ParityClub))
            {
                var partnerFit = partnersInfo.ConfigPartners.FirstOrDefault(x => x.Id == partner.PartnerCode);

                if (partnerFit == null)
                    continue;

                var legalTerms = await liveloRepository.GetPartnerLegalTerm(partnerFit.Id, cancellation);

                var bonificacao = $"Até {partner.ParityClub} pontos por real";

                if (partner.ParityClub == partner.Parity)
                    bonificacao = $"{partner.Parity} pontos por real";

                var partnerParity = new RetornoParity()
                {
                    Nome = partnerFit.Name,
                    Bonificacao = bonificacao,
                    Validade = GetDateFromLegalTerms(legalTerms),
                    LegalTerms = legalTerms
                };

                retorno.Add(partnerParity);
            }

            return retorno;
        }

        private static string GetDateFromLegalTerms(string? legalTerms)
        {
            if (legalTerms is null)
                return "Consultar regulamento";

            const string pattern = @"(\d{2})[-/](\d{2})[-/](\d{2,4})";

            var match = Regex.Match(legalTerms, pattern);

            if (!match.Success)
                return "Consultar regulamento";

            return match.Value;
        }
    }
}
