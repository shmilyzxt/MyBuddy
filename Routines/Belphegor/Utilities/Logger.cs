using System;
using System.Reflection;
using System.Windows.Media;
using Zeta.Common;

namespace Belphegor.Utilities
{
    public static class Logger
    {
        private static Version _version;
        private static readonly Color LogColor = Colors.Green;

        public static Version Version
        {
            get { return _version ?? (_version = Assembly.GetExecutingAssembly().GetName().Version); }
        }

        public static void WriteLog(LogLevel logLevel, Color clr, string message)
        {
            Logging.Write(logLevel, clr, "[{0} {1}] {2}", Belphegor.Instance.Name, Version, message);
        }

        public static void WriteLog(LogLevel logLevel, string format)
        {
            WriteLog(logLevel, format, new object[] {});
        }

        public static void WriteLog(LogLevel logLevel, string format, params object[] args)
        {
            WriteLog(logLevel, LogColor, format, args);
        }

        public static void WriteLog(LogLevel logLevel, Color clr, string format, params object[] args)
        {
            WriteLog(logLevel, clr, string.Format(format, args));
        }

        public static void Write(string message)
        {
            WriteLog(LogLevel.Normal, Colors.Green, message);
        }

        public static void Write(string message, params object[] args)
        {
            WriteLog(LogLevel.Normal, Colors.Green, message, args);
        }

        public static void Write(Color clr, string message, params object[] args)
        {
            WriteLog(LogLevel.Normal, clr, message, args);
        }

        public static void WriteVerbose(string message)
        {
            WriteLog(LogLevel.Verbose, message);
        }

        public static void WriteVerbose(string message, params object[] args)
        {
            WriteLog(LogLevel.Verbose, message, args);
        }

        public static void WriteVerbose(Color clr, string message, params object[] args)
        {
            WriteLog(LogLevel.Verbose, clr, message, args);
        }

        public static void WriteQuiet(string message)
        {
            WriteLog(LogLevel.Quiet, message);
        }

        public static void WriteQuiet(string message, params object[] args)
        {
            WriteLog(LogLevel.Quiet, message, args);
        }

        public static void WriteQuiet(Color clr, string message, params object[] args)
        {
            WriteLog(LogLevel.Quiet, clr, message, args);
        }
    }
}