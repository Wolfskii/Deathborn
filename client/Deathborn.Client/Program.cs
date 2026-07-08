using System.Text;
using Deathborn.Client;

static void LogCrash(Exception ex)
{
    try
    {
        var path = Path.Combine(AppContext.BaseDirectory, "deathborn-crash.log");
        var sb = new StringBuilder();
        sb.AppendLine(DateTime.UtcNow.ToString("O"));
        sb.AppendLine(ex.ToString());
        File.WriteAllText(path, sb.ToString());
    }
    catch
    {
        // ignore logging failures
    }
}

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is Exception ex)
        LogCrash(ex);
};

try
{
    using var game = new DeathbornGame();
    game.Run();
}
catch (Exception ex)
{
    LogCrash(ex);
    throw;
}
