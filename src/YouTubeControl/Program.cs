namespace YouTubeControl;

static class Program
{
    internal const string PipeName = "YouTubeControlPipe";
    private const string LeaderMutexName = "Global\\YouTubeControl_Leader_Mutex";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        var logger = new Logger(Path.Combine(AppContext.BaseDirectory, "logs.txt"));

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => HandleCriticalError(logger, e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            HandleCriticalError(logger, e.ExceptionObject as Exception ?? new Exception("Unknown unhandled error."));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.LogException("Unobserved task exception", e.Exception);
            e.SetObserved();
        };

        using var leaderMutex = new Mutex(false, LeaderMutexName);

        var isLeader = false;
        try
        {
            isLeader = leaderMutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            isLeader = true;
            logger.Log("Leader mutex was abandoned. Taking ownership.");
        }

        if (!isLeader)
        {
            MessengerMode.SendCommand(args, logger);
            return;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => cancellationTokenSource.Cancel();

        logger.Log("Leader started.");
        var leaderTask = LeaderMode.RunAsync(logger, cancellationTokenSource.Token);
        _ = leaderTask.ContinueWith(
            t => logger.LogException("Leader background task faulted", t.Exception ?? new Exception("Unknown task exception.")),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        cancellationTokenSource.Token.WaitHandle.WaitOne();

        leaderMutex.ReleaseMutex();
        logger.Log("Leader stopped.");
    }

    private static void HandleCriticalError(Logger logger, Exception exception)
    {
        logger.LogException("Critical process failure", exception);
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