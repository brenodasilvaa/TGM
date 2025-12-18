using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGM.Models;

namespace TGM.Services.Interfaces
{
    internal interface IParityServiceFactory
    {
        IParityService GetParityService(ParityProgram parityProgram);
    }
}
