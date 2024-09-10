using NLog;
using NLog.Config;
using NLog.Targets;

namespace Chrono.Core.Helpers
{
	public static class NLogHelper
	{
		private static ConsoleTarget _logConsoleTarget;
		private static ConsoleTarget _shortConsoleTarget;
		
		public static void ConfigureNLog()
		{
			var config = new LoggingConfiguration();

			_logConsoleTarget = new ConsoleTarget("logconsole")
			{
				Layout = "${longdate}|${level}| -> ${message} ${exception:format=tostring}"
			};
			_shortConsoleTarget = new ConsoleTarget("shortConsole")
			{
				Layout = "${level} -> ${message}"
			};
			
			config.AddTarget(_logConsoleTarget);
			config.AddTarget(_shortConsoleTarget);

			config.AddRule(LogLevel.Debug, LogLevel.Fatal, _logConsoleTarget);

			LogManager.Configuration = config;
		}
		
		public static void EnableShortConsoleTarget(bool enable = false)
		{
			var config = LogManager.Configuration;
			config.LoggingRules.Clear();

			if (enable)
			{
				config.AddRule(LogLevel.Trace, LogLevel.Fatal, _shortConsoleTarget);
			}
			else
			{
				config.AddRule(LogLevel.Trace, LogLevel.Fatal, _logConsoleTarget);
			}
			
			LogManager.ReconfigExistingLoggers();
		}
	}
}