using NLog;
using NLog.Config;
using NLog.Targets;

namespace Chrono.Core.Helpers
{

    public enum ChronoLogLevel
    {
        Debug,
        Trace
    }
    public static class NLogHelper
    {
        private static ConsoleTarget _logConsoleTarget = new("logconsole")
        {
            Layout = "${longdate} | ${level} | -> ${message} ${exception:format=tostring}"
        };

        private static ConsoleTarget _shortConsoleTarget = new("shortConsole")
        {
            Layout = "${level} -> ${message}"
        };

        public static void ConfigureNLog()
        {
            var config = new LoggingConfiguration();

            config.AddTarget(_logConsoleTarget);
            config.AddTarget(_shortConsoleTarget);
            LogManager.Configuration = config;
            
            SetLogLevel(false);
        }

        public static void SetLogLevel(bool enableTrace)
        {
            var config = LogManager.Configuration;
            config.LoggingRules.Clear();

            switch (enableTrace)
            {
                case true:
                    config.AddRule(LogLevel.Trace, LogLevel.Fatal, _shortConsoleTarget);
                    break;
                case false:
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, _logConsoleTarget);
                    break;
            }

            LogManager.ReconfigExistingLoggers();
        }
    }
}