using Microsoft.Extensions.Hosting;
using TGM.Services.Livelo;

namespace TGM.Workers
{
    internal class LiveloWorker(IParityService parityService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var parities = await parityService.GetParities(stoppingToken);
                var path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString("D") + ".csv");

                File.AppendAllText(path, $"Programa ; Parceiro ; Bonificação ; Validade; Termos Legais; {Environment.NewLine}");

                foreach (var test in parities)
                {
                    File.AppendAllText(path, $"Livelo ; {test.Nome} ; {test.Bonificacao} ; {test.Validade}; {test.LegalTerms}; {Environment.NewLine}", System.Text.Encoding.UTF8);
                }

                Console.WriteLine("Processamento concluído");
            }
            catch (Exception e)
            {
                Console.WriteLine("Algo errado ocorreu durante o processamento");
            }
            
        }
    }
}
