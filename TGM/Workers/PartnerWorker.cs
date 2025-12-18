using Microsoft.Extensions.Hosting;
using TGM.Models;
using TGM.Services.Interfaces;

namespace TGM.Workers
{
    internal class PartnerWorker(IParityServiceFactory parityServiceFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString("D") + ".csv");

                File.AppendAllText(path, $"Programa ; Parceiro ; Bonificação ; Validade; Termos Legais; {Environment.NewLine}");

                foreach (ParityProgram programa in Enum.GetValues(typeof(ParityProgram)))
                {
                    Console.WriteLine($"Processamento iniciado para o programa {programa}");

                    var parityService = parityServiceFactory.GetParityService(programa);

                    var parities = await parityService.GetParities(stoppingToken);

                    foreach (var test in parities)
                    {
                        File.AppendAllText(path, $"{programa} ; {test.Nome} ; {test.Pontuacao} ; {test.Validade}; {test.LegalTerms}; {Environment.NewLine}", System.Text.Encoding.UTF8);
                    }

                    Console.WriteLine("Processamento concluído");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Algo errado ocorreu durante o processamento");
            }
            
        }
    }
}
