using System.Net;
using System.Text.RegularExpressions;

namespace TgmCore.Helpers;

public static class StringManipulationHelper
{
    public static string CleanHtml(string input)
    {
        var noHtml = Regex.Replace(input, @"<[^>]+>", string.Empty);
        var decoded = WebUtility.HtmlDecode(noHtml);
        var cleaned = Regex.Replace(decoded, @"[\r\n]+", " ");
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim().Replace(";", ",");
        return ProcessarDescricao(cleaned);
    }

    public static string ProcessarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return descricao;

        var matchPromocional = Regex.Match(descricao,
            @"(\*.*?até\s+23h59min\s+do\s+dia\s+\d{2}/\d{2}/\d{4})",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (matchPromocional.Success)
            return Regex.Replace(matchPromocional.Value.Trim(), @"[\r\n]+", " ");

        var matchAcumulo = Regex.Match(descricao,
            @"O acúmulo padrão é de [^•\n]+?(?=\. Em períodos|\. O acúmulo|\. O crédito|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (matchAcumulo.Success)
            return matchAcumulo.Value.Trim().TrimEnd(',') + ".";

        var matchGenerico = Regex.Match(descricao,
            @"[Gg]anhe[^.•\n]+\.",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (matchGenerico.Success)
            return matchGenerico.Value.Trim();

        return "Consultar regulamento no site parceiro.";
    }
}
