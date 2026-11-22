/*
 * Simple application logger that writes timestamped messages to a daily rotating log. Logs older than
 * 14 days are automatically deleted. The logger also writes messages to the console. Log files are held
 * open for the duration of the application to avoid overhead from opening/closing them repeatedly.
 */

using System.Globalization;
using System.Runtime.InteropServices;

namespace TuneLift
{
    /// <summary>
    /// Simple application logger that writes timestamped messages to a daily rotating log.
    /// </summary>
    internal static class Logger
    {
        /// <summary>Writer for today's log file, or null when the logger is not open.</summary>
        private static StreamWriter? _writer;

        /// <summary>Stream under the writer, kept so that each write can seek to the end first.</summary>
        private static FileStream? _stream;

        /// <summary>True once Initialise has run and until Shutdown closes the log.</summary>
        private static bool _initialised;

        /// <summary>Guards the writer, the stream and the initialised flag.</summary>
        private static readonly Lock _lock = new();

        /// <summary>Keeps the SIGTERM handler registered for the life of the process.</summary>
        private static PosixSignalRegistration? _sigtermRegistration;

        /// <summary>Set to 1 by the first CTRL+C or SIGTERM so that repeated signals are handled once.</summary>
        private static int _signalHandled;

        /// <summary>Full path of today's log file, or null until the logger is initialised.</summary>
        public static string? LogFilePath { get; private set; }

        /// <summary>
        /// Initialise the logger with the specified log folder path. This will create the folder if it doesn't exist,
        /// open today's log file, and delete log files older than 14 days.
        /// </summary>
        /// <param name="logFolderPath">Path to folder containing logs</param>
        /// <exception cref="ArgumentException">Thrown when the log folder path is null or empty.</exception>
        public static void Initialise(string logFolderPath)
        {
            lock (_lock)
            {
                if (_initialised)
                    return;

                if (string.IsNullOrEmpty(logFolderPath))
                    throw new ArgumentException("logFolderPath cannot be null or empty", nameof(logFolderPath));

                Directory.CreateDirectory(logFolderPath);

                foreach (var file in Directory.GetFiles(logFolderPath, "*.log"))
                {
                    DateTime lastModified = File.GetLastWriteTime(file);
                    if ((DateTime.Now - lastModified).TotalDays > 14)
                        File.Delete(file);
                }

                string logFileName = $"log-{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log";
                string logFilePath = Path.Combine(logFolderPath, logFileName);
                LogFilePath = logFilePath;

                _stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _writer = new StreamWriter(_stream)
                {
                    AutoFlush = true
                };

                AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = false;

                    // A second signal while shutting down (e.g. from timeout, which also signals the process group) is ignored
                    if (Interlocked.Exchange(ref _signalHandled, 1) != 0)
                        return;

                    Write("CTRL+C pressed, shutting down...");
                    Shutdown();
                };
                _sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
                {
                    context.Cancel = false;
                    if (Interlocked.Exchange(ref _signalHandled, 1) != 0)
                        return;

                    Write("SIGTERM received, shutting down...");
                    Shutdown();
                });
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    // Make sure that the cursor is visible again if we crash
                    if (!Console.IsOutputRedirected)
                        Console.CursorVisible = true;

                    // Keep a record of the crash in the log file (file only, as the runtime reports it on the console)
                    Write($"Unhandled exception: {e.ExceptionObject}", true);
                };

                // Hide the console cursor to prevent it from distracting from log messages
                // (setting it throws when output is redirected)
                if (!Console.IsOutputRedirected)
                    Console.CursorVisible = false;

                _initialised = true;
            }
        }

        /// <summary>
        /// Write a message to the log file and console with a timestamp.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="verbose">True to write to the log file only, and to the console only when verbose mode is on.</param>
        public static void Write(string message, bool verbose = false)
        {
            lock (_lock)
            {
                if (_writer == null)
                    throw new InvalidOperationException("Logger not initialised. Call Logger.Initialise() first.");

                string tsDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string tsTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                string logEntry = $"[{tsDate} {tsTime}] {message}";

                // Another instance may have appended since our last write, so move to the end first or we would overwrite its lines
                _stream!.Seek(0, SeekOrigin.End);
                _writer.WriteLine(logEntry);

                if (!verbose || Globals.VerboseMode)
                {
                    Console.WriteLine($"[{tsTime}] {message}");
                }
            }
        }

        /// <summary>
        /// Close the log file cleanly. Called automatically on process exit and CTRL+C, so marked
        /// as private since it doesn't really need to be called explicitly.
        /// </summary>
        private static void Shutdown()
        {
            // Restore the console cursor visibility
            if (!Console.IsOutputRedirected)
                Console.CursorVisible = true;

            lock (_lock)
            {
                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                    _stream = null;
                }
                _initialised = false;
            }
        }
    }
}
