namespace TgmCore.Models.Esfera;

public class ParityInfoModel
{
    public string DisplayName { get; set; } = string.Empty;
    public ExternalInfo ExternalInfo { get; set; } = new();
    public string? esf_accumulationGeneralRules { get; set; }
    public string? esf_accumulationPrefix { get; set; }
    public string? esf_accumulationValue { get; set; }
}
