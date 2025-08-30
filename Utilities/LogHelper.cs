using NLog;

namespace GasFlowCalculator.Utilities
{
    /// <summary>
    /// Helper class for logging operations throughout the application
    /// </summary>
    public static class LogHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        public static void Debug(string category, string message)
        {
            logger.Debug($"[{category}] {message}");
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        public static void Info(string category, string message)
        {
            logger.Info($"[{category}] {message}");
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        public static void Warning(string category, string message)
        {
            logger.Warn($"[{category}] {message}");
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        public static void Error(string category, string message)
        {
            logger.Error($"[{category}] {message}");
        }

        /// <summary>
        /// Logs an error message with exception details
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        /// <param name="exception">Exception to log</param>
        public static void Error(string category, string message, Exception exception)
        {
            logger.Error(exception, $"[{category}] {message}");
        }

        /// <summary>
        /// Logs a fatal error message
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        public static void Fatal(string category, string message)
        {
            logger.Fatal($"[{category}] {message}");
        }

        /// <summary>
        /// Logs a fatal error message with exception details
        /// </summary>
        /// <param name="category">Category or method name</param>
        /// <param name="message">Log message</param>
        /// <param name="exception">Exception to log</param>
        public static void Fatal(string category, string message, Exception exception)
        {
            logger.Fatal(exception, $"[{category}] {message}");
        }

        /// <summary>
        /// Logs the start of a performance-critical operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Stopwatch for measuring operation duration</returns>
        public static System.Diagnostics.Stopwatch StartOperation(string operationName)
        {
            Debug("Performance", $"Starting operation: {operationName}");
            return System.Diagnostics.Stopwatch.StartNew();
        }

        /// <summary>
        /// Logs the completion of a performance-critical operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="stopwatch">Stopwatch from StartOperation</param>
        public static void EndOperation(string operationName, System.Diagnostics.Stopwatch stopwatch)
        {
            stopwatch.Stop();
            Debug("Performance", $"Completed operation: {operationName} in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}