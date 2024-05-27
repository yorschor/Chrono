using NLog;

namespace Chrono.Core.Helpers;

public static class ErrorResultExtensions
{
    public static void PrintAll(this IErrorResult errorResult)
    {
        foreach (var error in errorResult.Errors)
        {
            LogManager.GetCurrentClassLogger().Error($"{error.Code}: {error.Details}");
        }
    }
}