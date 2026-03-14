using System.Text.RegularExpressions;
using TgmCore.Models;
using TgmCore.Repositories;
using TgmCore.Services.Interfaces;

namespace TgmCore.Services.Livelo;

public class LiveloParityService(ILiveloRepository liveloRepository) : IParityService
{
    public async Task<List<RetornoParity>> GetParities(int valorMinimoPromocao, CancellationToken cancellation)
    {
        var partnersInfo = await liveloRepository.GetPartners(cancellation);
        var partnersParities = await liveloRepository.GetPartnersParities(cancellation);
        var retorno = new List<RetornoParity>();

        foreach (var partner in (partnersParities ?? []).Where(x => x.Promotion).OrderByDescending(x => x.ParityClub))
        {
            var partnerFit = partnersInfo?.ConfigPartners.FirstOrDefault(x => x.Id == partner.PartnerCode);
            if (partnerFit == null) continue;

            var legalTerms = await liveloRepository.GetPartnerLegalTerm(partnerFit.Id, cancellation);
            var bonificacao = partner.ParityClub == partner.Parity
                ? $"{partner.Parity} pontos por real"
                : $"Até {partner.ParityClub} pontos por real";

            retorno.Add(new RetornoParity
            {
                Nome = partnerFit.Name,
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
        var match = Regex.Match(legalTerms, @"(\d{2})[-/](\d{2})[-/](\d{2,4})");
        return match.Success ? match.Value : "Consultar regulamento";
    }
}
