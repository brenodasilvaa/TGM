using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGM.Models.Livelo
{
    internal class PartnerParityModel
    {
        public string PartnerCode { get; set; }
        public string Currency { get; set; }
        public double CurrencyValue { get; set; }
        public int Parity { get; set; }
        public int ParityClub { get; set; }
        public string LegalTerms { get; set; }
        public bool Promotion { get; set; }
    }
}
