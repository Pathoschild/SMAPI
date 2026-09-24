using System.Threading;

namespace StardewModdingAPI.Mobile;

public static class MobileConsoleTool
{
    static readonly object LineLock = new();
    static readonly ManualResetEventSlim LineReady = new(false);
    static string _currentLine = "";

    public static void WriteLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        lock (LineLock)
        {
            _currentLine = line;
            LineReady.Set();
        }
    }

    public static string ReadLine()
    {
        LineReady.Wait();

        lock (LineLock)
        {
            string line = _currentLine;
            _currentLine = "";
            LineReady.Reset();
            return line;
        }
    }
}
