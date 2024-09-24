using Huxy;
using NLog;

namespace Chrono.Core.Helpers;

public static class ErrorResultExtension
{
    public static void PrintErrors(this Result errorResult)
    {
        if (errorResult.Errors.Count == 0)
        {
            LogManager.GetCurrentClassLogger().Error(errorResult.Message);
        }
        foreach (var error in errorResult.Errors)
        {
            LogManager.GetCurrentClassLogger().Error($"{error.Code}: {error.Details}");
        }
    }
}