using Microsoft.Extensions.DependencyInjection;
using TgmCore.Models;
using TgmCore.Services.Esfera;
using TgmCore.Services.Interfaces;
using TgmCore.Services.Livelo;

namespace TgmCore.Services;

public class ParityServiceFactory(IServiceProvider serviceProvider) : IParityServiceFactory
{
    public IParityService GetParityService(ParityProgram parityProgram) => parityProgram switch
    {
        ParityProgram.Livelo => serviceProvider.GetRequiredService<LiveloParityService>(),
        ParityProgram.Esfera => serviceProvider.GetRequiredService<EsferaParityService>(),
        _ => throw new ArgumentOutOfRangeException(nameof(parityProgram), parityProgram, "ParityProgram não suportado")
    };
}
