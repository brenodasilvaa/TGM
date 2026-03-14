namespace PartnersPromoLambda.Services;

public interface IExecutionLimitService
{
    Task<bool> CanExecuteAsync();
    Task IncrementExecutionCountAsync();
    Task<int> GetRemainingExecutionsAsync();
}
