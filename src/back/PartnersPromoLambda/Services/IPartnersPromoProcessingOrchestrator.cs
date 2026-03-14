using TgmCore.Models;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public interface IPartnersPromoProcessingOrchestrator
{
    Task<IEnumerable<RetornoParity>> ProcessAllParitiesAsync(int minimumScore, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParityResultWithProgram>> ProcessAllParitiesWithProgramAsync(int minimumScore, CancellationToken cancellationToken = default);
}
