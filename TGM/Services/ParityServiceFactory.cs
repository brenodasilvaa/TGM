using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGM.Models;
using TGM.Services.Esfera;
using TGM.Services.Interfaces;
using TGM.Services.Livelo;

namespace TGM.Services
{
    internal class ParityServiceFactory(IServiceProvider serviceProvider) : IParityServiceFactory
    {
        public IParityService GetParityService(ParityProgram parityProgram)
        {
            return parityProgram switch
            {
                ParityProgram.Livelo =>
                    serviceProvider.GetRequiredService<LiveloParityService>(),

                ParityProgram.Esfera =>
                    serviceProvider.GetRequiredService<EsferaParityService>(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(parityProgram),
                    parityProgram,
                    "ParityProgram não suportado"
                )
            };
        }
    }
}
