using Microsoft.Extensions.Hosting;
using TgmCore.Models;
using TgmCore.Services.Interfaces;

namespace TGM.Workers;

internal class PartnerWorker(IParityServiceFactory parityServiceFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString("D") + ".csv");
            File.AppendAllText(path, $"Programa ; Parceiro ; Bonificação ; Validade; Termos Legais; {Environment.NewLine}");

            Console.WriteLine("Insira o valor mínimo considerado promoção ou pressione Enter para considerar o padrão 4");
            var resposta = Console.ReadLine();

            if (!int.TryParse(resposta, out var limiteMinimo) || limiteMinimo < 1)
                limiteMinimo = 4;

            foreach (ParityProgram programa in Enum.GetValues(typeof(ParityProgram)))
            {
                Console.WriteLine($"Processamento iniciado para o programa {programa}");
                var parityService = parityServiceFactory.GetParityService(programa);
                var parities = await parityService.GetParities(limiteMinimo, stoppingToken);

                foreach (var parity in parities)
                    File.AppendAllText(path, $"{programa} ; {parity.Nome} ; {parity.Pontuacao} ; {parity.Validade}; {parity.LegalTerms}; {Environment.NewLine}", System.Text.Encoding.UTF8);

                Console.WriteLine("Processamento concluído");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Algo errado ocorreu durante o processamento");
        }
    }
}
