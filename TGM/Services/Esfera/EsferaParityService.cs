using System.Text.RegularExpressions;
using TGM.Models;
using TGM.Repositories;
using TGM.Services.Interfaces;

namespace TGM.Services.Esfera
{
    internal class EsferaParityService(IEsferaRepository esferaRepository) : IParityService
    {
        public async Task<List<RetornoParity>> GetParities(CancellationToken cancellation)
        {
            Console.WriteLine("Obtendo lista de promoções...");
            var partnersParities = await esferaRepository.GetPartnersParities(cancellation);

            var retorno = new List<RetornoParity>();

            foreach (var partner in partnersParities)
            {
                if (partner.esf_accumulationValue == null || partner.esf_accumulationValue < 4)
                    continue;

                var bonificacao = $"Até {partner.esf_accumulationValue} pontos por real";

                var partnerParity = new RetornoParity()
                {
                    Nome = partner.DisplayName,
                    Pontuacao = bonificacao,
                    Validade = GetDateFromLegalTerms(partner.esf_accumulationGeneralRules),
                    LegalTerms = partner.esf_accumulationGeneralRules
                };

                retorno.Add(partnerParity);
            }

            return retorno;
        }

        private static string GetDateFromLegalTerms(string? legalTerms)
        {
            if (legalTerms is null)
                return "Consultar regulamento";

            const string pattern = @"até\s+\d{2}h\d{2}min\s+do\s+dia\s+(\d{2}\/\d{2}\/\d{4})";

            var match = Regex.Match(legalTerms, pattern);

            if (!match.Success)
                return "Consultar regulamento";

            return match.Groups[1].Value;
        }
    }
}
