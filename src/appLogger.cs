using System;

namespace Yugioh
{
    public static class AppLogger
    {
        private static readonly object consoleLock = new object();

        public static void Info(string component, string message)
        {
            Write("INFO", component, message);
        }

        public static void Warn(string component, string message)
        {
            Write("WARN", component, message);
        }

        public static void Error(string component, string message)
        {
            Write("ERROR", component, message);
        }

        public static void Error(string component, string message, Exception ex)
        {
            Write("ERROR", component, $"{message} | Exception={ex.Message}");
        }

        private static void Write(string level, string component, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            lock (consoleLock)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ResolveColor(level);
                Console.WriteLine($"[{timestamp}] [{level}] [{component}] {message}");
                Console.ForegroundColor = originalColor;
            }
        }

        private static ConsoleColor ResolveColor(string level)
        {
            return level switch
            {
                "WARN" => ConsoleColor.Yellow,
                "ERROR" => ConsoleColor.Red,
                _ => ConsoleColor.Green
            };
        }
    }
}
