namespace TGM.Helpers
{
    public class StringManipulationHelper
    {
        public static string? FilterLegalTermsUntilFirstPeriod(string? legalTerms)
        {
            if (string.IsNullOrEmpty(legalTerms))
                return legalTerms;

            var firstPeriodIndex = legalTerms.IndexOf('.');

            if (firstPeriodIndex == -1)
                return legalTerms;

            return legalTerms.Substring(0, firstPeriodIndex);
        }
    }
}
