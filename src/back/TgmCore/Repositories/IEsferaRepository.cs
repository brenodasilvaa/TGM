using TgmCore.Models.Esfera;

namespace TgmCore.Repositories;

public interface IEsferaRepository
{
    Task<List<ParityInfoModel>?> GetPartnersParities(CancellationToken cancellation);
}
