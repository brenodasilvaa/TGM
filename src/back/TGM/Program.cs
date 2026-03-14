using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgmCore;
using TGM.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTgmCore();
builder.Services.AddHostedService<PartnerWorker>();

IHost host = builder.Build();
host.Run();
