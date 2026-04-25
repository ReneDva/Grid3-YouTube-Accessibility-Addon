// Entry point and role selector for YouTubeControl.
// Chooses Leader or Messenger mode with a global mutex and named pipe.
// Contains the Program class and global startup/error lifecycle flow.
namespace YouTubeControl;

/// <summary>
/// Coordinates process startup, role selection, and shutdown for YouTubeControl.
/// </summary>
/// <remarks>
/// Initializes logging and exception hooks, then either forwards a command to the active leader
/// instance or starts the leader loop and waits for a shutdown signal.
/// </remarks>
static class Program
{
    private const string ComponentName = "Program";

    // Shared named pipe for leader and messenger.
    internal const string PipeName = "YouTubeControlPipe";

    // Global mutex for single leader election.
    private const string LeaderMutexName = "Global\\YouTubeControl_Leader_Mutex";

    /// <summary>
    /// Starts the application and chooses leader or messenger behavior.
    /// </summary>
    /// <param name="args">The command arguments received by this process instance.</param>
    [STAThread]
    static void Main(string[] args)
    {
        // Initialize WinForms app defaults.
        ApplicationConfiguration.Initialize();

        var logger = new Logger(Path.Combine(AppContext.BaseDirectory, "logs.txt"));

        // Register global crash handlers.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => HandleCriticalError(logger, e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            HandleCriticalError(logger, e.ExceptionObject as Exception ?? new Exception("Unknown unhandled error."));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.LogException(ComponentName, "Unobserved task exception", e.Exception);
            e.SetObserved();
        };

        // Try to become the leader instance.
        using var leaderMutex = new Mutex(false, LeaderMutexName);

        var isLeader = false;
        try
        {
            isLeader = leaderMutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            isLeader = true;
            logger.Log(ComponentName, "Leader mutex was abandoned. Taking ownership.");
        }

        if (!isLeader)
        {
            // Forward args to the current leader.
            MessengerMode.SendCommand(args, logger);
            return;
        }

        // Start leader lifetime token.
        using var cancellationTokenSource = new CancellationTokenSource();
        // Cancel leader loop on process exit.
        EventHandler processExitHandler = (_, _) => TryCancel(cancellationTokenSource);
        AppDomain.CurrentDomain.ProcessExit += processExitHandler;

        // Stop leader when command dispatcher signals explicit exit/stop.
        Action leaderShutdownHandler = () =>
        {
            logger.Log(ComponentName, "Leader shutdown requested by command.");
            TryCancel(cancellationTokenSource);
        };
        LeaderMode.ShutdownRequested += leaderShutdownHandler;

        logger.Log(ComponentName, "Leader started.");
        var leaderTask = LeaderMode.RunAsync(logger, cancellationTokenSource.Token);
        _ = leaderTask.ContinueWith(
            t =>
            {
                logger.LogException(ComponentName, "Leader background task faulted", t.Exception ?? new Exception("Unknown task exception."));
                TryCancel(cancellationTokenSource);
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        // Keep process alive until shutdown.
        cancellationTokenSource.Token.WaitHandle.WaitOne();

        AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
        LeaderMode.ShutdownRequested -= leaderShutdownHandler;
        leaderMutex.ReleaseMutex();
        logger.Log(ComponentName, "Leader stopped.");
    }

    private static void TryCancel(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // Safe to ignore during shutdown races.
        }
    }

    /// <summary>
    /// Logs a critical failure and requests application shutdown.
    /// </summary>
    /// <param name="logger">The logger instance used for crash reporting.</param>
    /// <param name="exception">The unhandled exception that triggered this path.</param>
    private static void HandleCriticalError(Logger logger, Exception exception)
    {
        logger.LogException(ComponentName, "Critical process failure", exception);
        try
        {
            Application.Exit();
        }
        catch
        {
            // Swallow all failures in crash path to avoid secondary crashes.
        }
    }
}