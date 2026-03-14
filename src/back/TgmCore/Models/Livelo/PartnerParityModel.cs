namespace TgmCore.Models.Livelo;

public class PartnerParityModel
{
    public string PartnerCode { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public double CurrencyValue { get; set; }
    public int Parity { get; set; }
    public int ParityClub { get; set; }
    public string LegalTerms { get; set; } = string.Empty;
    public bool Promotion { get; set; }
}
