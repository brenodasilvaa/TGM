using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGM.Models.Livelo;

namespace TGM.Repositories
{
    internal interface ILiveloRepository
    {
        public Task<Partner?> GetPartners(CancellationToken cancellation);
        Task<List<PartnerParityModel>?> GetPartnersParities(CancellationToken cancellation);
        public Task<string?> GetPartnerLegalTerm(string partnerCode, CancellationToken cancellation);
    }
}
