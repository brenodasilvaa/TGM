using TGM.Models;
using TGM.Repositories;
using TGM.Services.Interfaces;

namespace TGM.Services.Esfera
{
    internal class EsferaParityService(IEsferaRepository esferaRepository) : IParityService
    {
        public Task<List<RetornoParity>> GetParities(CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }
}
