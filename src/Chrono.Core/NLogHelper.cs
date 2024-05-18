using NLog;
using NLog.Config;
using NLog.Targets;

namespace Chrono.Core;

public class NLogHelper
{
    public static void EnableShortConsoleTarget(bool enable = false)
    {
        var config = LogManager.Configuration;
        var targetName = "shortConsole";

        var existingTarget = config.FindTargetByName(targetName);

        if (!enable)
        {
            if (existingTarget is not null)
            {
                var rulesToRemove = config.LoggingRules.Where(r => r.Targets.Contains(existingTarget)).ToList();
                foreach (var rule in rulesToRemove)
                {
                    config.LoggingRules.Remove(rule);
                }
            }
        }
        else
        {
            var rule = new LoggingRule("*", LogLevel.Trace, existingTarget);
            config.LoggingRules.Add(rule);
        }

        LogManager.Configuration = config;
    }
}