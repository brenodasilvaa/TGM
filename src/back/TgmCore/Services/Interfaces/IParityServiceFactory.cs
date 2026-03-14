using TgmCore.Models;

namespace TgmCore.Services.Interfaces;

public interface IParityServiceFactory
{
    IParityService GetParityService(ParityProgram parityProgram);
}
