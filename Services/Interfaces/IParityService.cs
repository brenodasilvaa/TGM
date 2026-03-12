using TGM.Models;

namespace TGM.Services.Interfaces
{
    public interface IParityService
    {
        Task<List<RetornoParity>> GetParities(int valorMinimoPromocao, CancellationToken cancellation);
    }
}
