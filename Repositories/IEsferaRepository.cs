using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGM.Models.Esfera;
using TGM.Models.Livelo;

namespace TGM.Repositories
{
    public interface IEsferaRepository
    {
        Task<List<ParityInfoModel>?> GetPartnersParities(CancellationToken cancellation);
    }
}
