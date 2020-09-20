using System;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
// ReSharper disable EmptyGeneralCatchClause

namespace ElectionResults.Core.Infrastructure
{
    public static class Log
    {
        private static ILogger _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public static void LogInformation(string message)
        {
            try
            {
                Console.WriteLine(message);
                _logger.LogInformation(message);
            }
            catch
            {
            }
        }

        public static void LogError(Exception exception, string message = null)
        {
            try
            {
                Console.WriteLine(exception.ToString());
                Console.WriteLine(message);
                _logger.LogError(exception, message);
            }
            catch
            {
            }
        }

        public static void LogError(string message)
        {
            try
            {
                Console.WriteLine(message);
                _logger.LogError(message);
            }
            catch
            {
            }
        }

        public static void LogWarning(string message)
        {
            try
            {
                _logger.LogWarning(message);
            }
            catch
            {
                
            }
        }
    }
}
