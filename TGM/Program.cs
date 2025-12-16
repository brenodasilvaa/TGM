using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TGM.Repositories;
using TGM.Services.Interfaces;
using TGM.Services.Livelo;
using TGM.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<LiveloWorker>();
builder.Services.AddScoped<ILiveloRepository, LiveloRepository>();
builder.Services.AddScoped<IParityService, LiveloParityService>();

IHost host = builder.Build();

host.Run();