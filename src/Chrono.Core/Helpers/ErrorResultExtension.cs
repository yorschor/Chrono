using Huxy;
using NLog;

namespace Chrono.Core.Helpers;

public static class ErrorResultExtension
{
    public static void PrintErrors(this Result errorResult)
    {
        foreach (var error in errorResult.Errors)
        {
            LogManager.GetCurrentClassLogger().Error($"{error.Code}: {error.Details}");
        }
    }
}