using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGM.Services.Livelo;

namespace TGM.Workers
{
    internal class LiveloWorker(IParityService parityService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var teste = await parityService.GetParities(stoppingToken);
            var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".txt");

            foreach (var test in teste) 
            {
                File.AppendAllText(path, $"Programa: Livelo | Parceiro: {test.Nome} | Bonificação: {test.Bonificacao} | Validade: {test.Validade} {Environment.NewLine}");
            }
        }
    }
}
