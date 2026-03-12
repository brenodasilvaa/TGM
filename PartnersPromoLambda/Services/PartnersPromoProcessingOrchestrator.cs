using TGM.Models;
using TGM.Services.Interfaces;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public class PartnersPromoProcessingOrchestrator : IPartnersPromoProcessingOrchestrator
{
    private readonly IParityServiceFactory _parityServiceFactory;

    public PartnersPromoProcessingOrchestrator(IParityServiceFactory parityServiceFactory)
    {
        _parityServiceFactory = parityServiceFactory;
    }

    public async Task<IEnumerable<RetornoParity>> ProcessAllParitiesAsync(
        int minimumScore, 
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<RetornoParity>();

        // Iterate through all ParityProgram enum values
        foreach (ParityProgram program in Enum.GetValues(typeof(ParityProgram)))
        {
            var parityService = _parityServiceFactory.GetParityService(program);
            var results = await parityService.GetParities(minimumScore, cancellationToken);
            
            // Note: minimumScore filtering could be applied here if needed
            // For now, collecting all results as the TGM services don't use minimumScore parameter
            allResults.AddRange(results);
        }

        return allResults;
    }

    public async Task<IEnumerable<ParityResultWithProgram>> ProcessAllParitiesWithProgramAsync(
        int minimumScore, 
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<ParityResultWithProgram>();

        // Iterate through all ParityProgram enum values
        foreach (ParityProgram program in Enum.GetValues(typeof(ParityProgram)))
        {
            var parityService = _parityServiceFactory.GetParityService(program);
            var results = await parityService.GetParities(minimumScore, cancellationToken);
            
            // Add program information to each result
            allResults.AddRange(results.Select(r => new ParityResultWithProgram
            {
                Program = program,
                Result = r
            }));
        }

        return allResults;
    }
}
