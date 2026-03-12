namespace PartnersPromoLambda.Services;

public class ValidationService
{
    private readonly string _verificationCode;

    public ValidationService(string verificationCode)
    {
        _verificationCode = verificationCode;
    }

    public bool ValidateVerificationCode(string code)
    {
        return !string.IsNullOrWhiteSpace(code) && code == _verificationCode;
    }

    public (bool IsValid, string? ErrorMessage) ValidateMinimumScore(int minimumScore)
    {
        if (minimumScore <= 0)
        {
            return (false, "Minimum score must be a positive number");
        }

        return (true, null);
    }
}
