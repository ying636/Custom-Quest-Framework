using System;
using UnityEngine;

internal sealed class ChecksLogHandler : ILogHandler
{
    public static int ErrorCount { get; private set; }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        ErrorCount++;
        Console.WriteLine(exception);
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        if (logType == LogType.Error || logType == LogType.Exception) ErrorCount++;
        Console.WriteLine(logType + ": " + string.Format(format, args));
    }
}
