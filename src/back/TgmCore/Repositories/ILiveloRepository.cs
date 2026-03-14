using TgmCore.Models.Livelo;

namespace TgmCore.Repositories;

public interface ILiveloRepository
{
    Task<Partner?> GetPartners(CancellationToken cancellation);
    Task<List<PartnerParityModel>?> GetPartnersParities(CancellationToken cancellation);
    Task<string?> GetPartnerLegalTerm(string partnerCode, CancellationToken cancellation);
}
