using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TGM.Repositories;
using TGM.Services;
using TGM.Services.Esfera;
using TGM.Services.Interfaces;
using TGM.Services.Livelo;
using TGM.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<PartnerWorker>();
builder.Services.AddScoped<ILiveloRepository, LiveloRepository>();
builder.Services.AddScoped<IEsferaRepository, EsferaRepository>();
builder.Services.AddScoped<EsferaParityService>();
builder.Services.AddScoped<LiveloParityService>();
builder.Services.AddScoped<IParityServiceFactory, ParityServiceFactory>();


IHost host = builder.Build();

host.Run();