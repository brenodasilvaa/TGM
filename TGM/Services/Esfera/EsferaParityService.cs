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

            Console.WriteLine("Insira o valor mínimo considerado promoção ou pressione Enter para considerar o padrão 4");
            
            var resposta = Console.ReadLine();

            if (!int.TryParse(resposta, out var limiteMinimo) || limiteMinimo < 1)
                limiteMinimo = 4;

            foreach (var partner in partnersParities.Where(x => x.esf_accumulationValue is not null)
                                                    .OrderByDescending(x => int.Parse(x.esf_accumulationValue)))
            {
                if (int.Parse(partner.esf_accumulationValue) < limiteMinimo)
                    continue;

                var bonificacao = $"{partner.esf_accumulationPrefix} {partner.esf_accumulationValue} pontos por real";
                var legalTerms = partner.esf_accumulationGeneralRules is not null ? Regex.Replace(partner.esf_accumulationGeneralRules, "<.*?>|\\r?\\n", string.Empty).Replace(";", string.Empty) : string.Empty;

                var partnerParity = new RetornoParity()
                {
                    Nome = partner.DisplayName,
                    Pontuacao = bonificacao,
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

            const string pattern = @"até\s+\d{2}h\d{2}min\s+do\s+dia\s+(\d{2}\/\d{2}\/\d{4})";

            var match = Regex.Match(legalTerms, pattern);

            if (!match.Success)
                return "Consultar regulamento";

            return match.Groups[1].Value;
        }
    }
}
