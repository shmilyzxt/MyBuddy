﻿using System.Windows.Media;
using Zeta.Common;

namespace GilesTrinity.Technicals
{
    /// <summary>
    /// Utilities help developer interact with DemonBuddy
    /// </summary>
    internal static class DbHelper
    {
        /// <summary>Logs the specified level.</summary>
        /// <param name="level">The logging level.</param>
        /// <param name="category">The category.</param>
        /// <param name="formatMessage">The format message.</param>
        /// <param name="args">The parameters used when format message.</param>
        public static void Log(TrinityLogLevel level, LogCategory category, string formatMessage, params object[] args)
        {
            if (category == LogCategory.UserInformation || level >= TrinityLogLevel.Error || (GilesTrinity.Settings != null && GilesTrinity.Settings.Advanced.LogCategories.HasFlag(category)))
            {
                string msg = string.Format("[Trinity]{0}{1}", category != LogCategory.UserInformation ? "[" + category.ToString() + "]" : string.Empty, formatMessage);
                if (level == TrinityLogLevel.Critical)
                {
                    Logging.Write(ConvertToLogLevel(level), Colors.Red, msg, args);
                }
                else
                {
                    Logging.Write(ConvertToLogLevel(level), msg, args);
                }
            }
        }

        /// <summary>Logs the message to Normal log level</summary>
        /// <param name="category">The category.</param>
        /// <param name="formatMessage">The format message.</param>
        /// <param name="args">The parameters used when format message.</param>
        public static void Log(LogCategory category, string formatMessage, params object[] args)
        {
            Log(TrinityLogLevel.Normal, category, formatMessage, args);
        }

        /// <summary>
        /// Logs a message with Normal/UserInformation
        /// </summary>
        /// <param name="formatMessage"></param>
        /// <param name="args"></param>
        public static void LogNormal(string formatMessage, params object[] args)
        {
            Log(TrinityLogLevel.Normal, LogCategory.UserInformation, formatMessage, args);
        }

        /// <summary>
        /// Logs a message with Normal/UserInformation
        /// </summary>
        /// <param name="formatMessage"></param>
        /// <param name="args"></param>
        public static void LogVerbose(string formatMessage, params object[] args)
        {
            Log(TrinityLogLevel.Verbose, LogCategory.UserInformation, formatMessage, args);
        }

        /// <summary>
        /// Logs a message with Normal/UserInformation
        /// </summary>
        /// <param name="formatMessage"></param>
        /// <param name="args"></param>
        public static void LogDebug(string formatMessage, params object[] args)
        {
            Log(TrinityLogLevel.Debug, LogCategory.UserInformation, formatMessage, args);
        }

        /// <summary>Converts <see cref="TrinityLogLevel"/> to <see cref="LogLevel"/>.</summary>
        /// <param name="level">The trinity logging level.</param>
        /// <returns>DemonBuddy logging level.</returns>
        private static LogLevel ConvertToLogLevel(TrinityLogLevel level)
        {
            LogLevel logLevel = LogLevel.Diagnostic;
            switch (level)
            {
                case TrinityLogLevel.Critical:
                    logLevel = LogLevel.None;
                    break;
                case TrinityLogLevel.Error:
                    logLevel = LogLevel.Quiet;
                    break;
                case TrinityLogLevel.Normal:
                    logLevel = LogLevel.Normal;
                    break;
                case TrinityLogLevel.Verbose:
                    logLevel = LogLevel.Verbose;
                    break;
                case TrinityLogLevel.Debug:
                    logLevel = LogLevel.Diagnostic;
                    break;
            }
            return logLevel;
        }
    }
}
