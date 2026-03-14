using TgmCore.Models;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public interface ICsvGenerationService
{
    Task<string> GenerateCsvAsync(IEnumerable<RetornoParity> parityResults, string fileName);
    Task<string> GenerateCsvWithProgramAsync(IEnumerable<ParityResultWithProgram> parityResults);
}
