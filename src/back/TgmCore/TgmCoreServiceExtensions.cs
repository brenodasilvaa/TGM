using Microsoft.Extensions.DependencyInjection;
using TgmCore.Repositories;
using TgmCore.Services;
using TgmCore.Services.Esfera;
using TgmCore.Services.Interfaces;
using TgmCore.Services.Livelo;

namespace TgmCore;

public static class TgmCoreServiceExtensions
{
    public static IServiceCollection AddTgmCore(this IServiceCollection services)
    {
        services.AddScoped<ILiveloRepository, LiveloRepository>();
        services.AddScoped<IEsferaRepository, EsferaRepository>();
        services.AddScoped<LiveloParityService>();
        services.AddScoped<EsferaParityService>();
        services.AddScoped<IParityServiceFactory, ParityServiceFactory>();
        return services;
    }
}
