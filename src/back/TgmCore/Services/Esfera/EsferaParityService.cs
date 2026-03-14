using System.Text.RegularExpressions;
using TgmCore.Helpers;
using TgmCore.Models;
using TgmCore.Repositories;
using TgmCore.Services.Interfaces;

namespace TgmCore.Services.Esfera;

public class EsferaParityService(IEsferaRepository esferaRepository) : IParityService
{
    public async Task<List<RetornoParity>> GetParities(int valorMinimoPromocao, CancellationToken cancellation)
    {
        var partnersParities = await esferaRepository.GetPartnersParities(cancellation);
        var retorno = new List<RetornoParity>();

        foreach (var partner in (partnersParities ?? [])
            .Where(x => x.esf_accumulationValue is not null)
            .OrderByDescending(x => double.Parse(x.esf_accumulationValue!)))
        {
            if (double.Parse(partner.esf_accumulationValue!) < valorMinimoPromocao)
                continue;

            var bonificacao = $"{partner.esf_accumulationPrefix} {partner.esf_accumulationValue} pontos por real";
            var legalTerms = partner.esf_accumulationGeneralRules is not null
                ? StringManipulationHelper.CleanHtml(partner.esf_accumulationGeneralRules)
                : string.Empty;

            retorno.Add(new RetornoParity
            {
                Nome = partner.DisplayName,
                Pontuacao = bonificacao,
                Validade = GetDateFromLegalTerms(legalTerms),
                LegalTerms = legalTerms
            });
        }

        return retorno;
    }

    private static string GetDateFromLegalTerms(string? legalTerms)
    {
        if (legalTerms is null) return "Consultar regulamento";
        var match = Regex.Match(legalTerms, @"até\s+\d{2}h\d{2}min\s+do\s+dia\s+(\d{2}\/\d{2}\/\d{4})");
        return match.Success ? match.Groups[1].Value : "Consultar regulamento";
    }
}
