using TgmCore.Models;
using TgmCore.Services.Interfaces;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public class PartnersPromoProcessingOrchestrator(IParityServiceFactory parityServiceFactory) : IPartnersPromoProcessingOrchestrator
{
    public async Task<IEnumerable<RetornoParity>> ProcessAllParitiesAsync(int minimumScore, CancellationToken cancellationToken = default)
    {
        var allResults = new List<RetornoParity>();
        foreach (ParityProgram program in Enum.GetValues(typeof(ParityProgram)))
        {
            var results = await parityServiceFactory.GetParityService(program).GetParities(minimumScore, cancellationToken);
            allResults.AddRange(results);
        }
        return allResults;
    }

    public async Task<IEnumerable<ParityResultWithProgram>> ProcessAllParitiesWithProgramAsync(int minimumScore, CancellationToken cancellationToken = default)
    {
        var allResults = new List<ParityResultWithProgram>();
        foreach (ParityProgram program in Enum.GetValues(typeof(ParityProgram)))
        {
            var results = await parityServiceFactory.GetParityService(program).GetParities(minimumScore, cancellationToken);
            allResults.AddRange(results.Select(r => new ParityResultWithProgram { Program = program, Result = r }));
        }
        return allResults;
    }
}
