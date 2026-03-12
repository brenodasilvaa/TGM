using TGM.Models;

namespace PartnersPromoLambda.Services;

public interface ICsvGenerationService
{
    Task<string> GenerateCsvAsync(IEnumerable<RetornoParity> parityResults, string fileName);
}
