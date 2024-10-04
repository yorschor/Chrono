using Huxy;
using NLog;
using Nuke.Common.Utilities;

namespace Chrono.Core.Helpers;

public static class ResultExtension
{
    public static void PrintErrors(this IResult errorResult)
    {
        var logger = LogManager.GetCurrentClassLogger();
        if (!string.IsNullOrEmpty(errorResult.Message))
        {
            logger.Error(errorResult.Message);
            if (errorResult.Exception != null && logger.IsTraceEnabled)
            {
                logger.Error(errorResult.Exception.ToString());
            }
        }
    }
}