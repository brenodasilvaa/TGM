using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TGM.Models.Livelo;
using TGM.Repositories;

namespace TGM.Services.Livelo
{
    internal class ParityService(ILiveloRepository liveloRepository) : IParityService
    {
        public async Task<List<RetornoParity>> GetParities(CancellationToken cancellation)
        {
            var partnersInfo = await liveloRepository.GetPartners(cancellation);
            var partnersParities = await liveloRepository.GetPartnersParities(cancellation);

            var retorno = new List<RetornoParity>();

            foreach (var partner in partnersParities.Where(x => x.Promotion).OrderByDescending(x => x.ParityClub))
            {
                var partnerFit = partnersInfo.Partners.FirstOrDefault(x => x.Id == partner.PartnerCode);

                if (partnerFit == null || !partnerFit.Active)
                    continue;

                var partnerParity = new RetornoParity()
                {
                    Nome = partnerFit.Name,
                    Bonificacao = $"Até {partner.Currency}{partner.Parity} ou Até {partner.Currency}{partner.ParityClub} no Clube Livelo",
                    Validade = GetDateFromLegalTerms(await liveloRepository.GetPartnerLegalTerm(partnerFit.Id, cancellation))
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
