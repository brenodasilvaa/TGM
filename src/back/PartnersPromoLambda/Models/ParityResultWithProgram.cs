using TgmCore.Models;

namespace PartnersPromoLambda.Models;

public class ParityResultWithProgram
{
    public ParityProgram Program { get; set; }
    public RetornoParity Result { get; set; } = new();
}
