using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGM.Models.Livelo;

namespace TGM.Services.Livelo
{
    internal interface IParityService
    {
        Task<List<RetornoParity>> GetParities(CancellationToken cancellation);
    }
}
