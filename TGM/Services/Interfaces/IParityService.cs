using TGM.Models;

namespace TGM.Services.Interfaces
{
    internal interface IParityService
    {
        Task<List<RetornoParity>> GetParities(CancellationToken cancellation);
    }
}
