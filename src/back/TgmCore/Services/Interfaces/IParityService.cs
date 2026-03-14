using TgmCore.Models;

namespace TgmCore.Services.Interfaces;

public interface IParityService
{
    Task<List<RetornoParity>> GetParities(int valorMinimoPromocao, CancellationToken cancellation);
}
