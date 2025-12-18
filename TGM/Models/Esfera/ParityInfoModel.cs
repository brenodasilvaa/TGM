using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGM.Models.Esfera
{
    internal class ParityInfoModel
    {
        public string DisplayName { get; set; }
        public ExternalInfo ExternalInfo { get; set; }
        public string esf_accumulationGeneralRules { get; set; }
        public string esf_accumulationPrefix { get; set; }
        public int? esf_accumulationValue { get; set; }
    }
}
