using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class MessageLoggedEventArgs : EventArgs {

    public DateTime Timestamp { get; private set; }
    public int LogLevel { get; private set; }
    public string Message { get; private set; }

    internal MessageLoggedEventArgs(DateTime timestamp, int logLevel, string message) {
        Timestamp = timestamp;
        LogLevel = logLevel;
        Message = message;
    }

}

/// <summary>
/// A utility class for creating log files to store messages with different log levels.
/// </summary>
public class Log : IDisposable {

    #region static

    public static string LogFilePath = "Logs/";
    private static string logFilename = "log.txt";

    public static int LogLevel = LevelDebug;

    public const int LevelError = 0;
    public const int LevelWarning = 1;
    public const int LevelInfo = 2;
    public const int LevelVerbose = 3;
    public const int LevelDebug = 4;

    public static readonly string PrefixError = "E";
    public static readonly string PrefixWarning = "W";
    public static readonly string PrefixInfo = "I";
    public static readonly string PrefixVerbose = "V";
    public static readonly string PrefixDebug = "D";


    private static Log instance;

    public static Log Instance {
        get {
            if (instance == null)
                instance = new Log();
            return instance;
        }
    }

    public delegate void MessageLoggedHandler(Log sender, MessageLoggedEventArgs args);
    public static event MessageLoggedHandler OnMessageLogged;
    
    public static void Debug(string format, params object[] args) {
        if (LogLevel >= LevelDebug)
            Instance.Write(LevelDebug, format, args);
    }

    public static void Verbose(string format, params object[] args) {
        if (LogLevel >= LevelVerbose)
            Instance.Write(LevelVerbose, format, args);
    }

    public static void Info(string format, params object[] args) {
        if (LogLevel >= LevelInfo)
            Instance.Write(LevelInfo, format, args);
    }

    public static void Warn(string format, params object[] args) {
        if (LogLevel >= LevelWarning)
            Instance.Write(LevelWarning, format, args);
    }

    public static void Error(string format, params object[] args) {
        if (LogLevel >= LevelError)
            Instance.Write(LevelError, format, args);
    }

    public static void Error(Exception e) {
        if (LogLevel >= LevelError)
            Instance.WriteError(e);
    }

    //public static void Error(string format, params object[] args) {
    //    Instance.Write(PREFIX_ERROR, format, args);
    //}

    #endregion static

    #region instance

    private StreamWriter sw;

    private readonly string timestampFormat = "{0:HH:mm:ss.fff}";
    private readonly string lineFormat = "[{0}] [{1}] {2}";

    public string FilePath { get; private set; }

    private Log() {
        DateTime now = DateTime.Now;
        //string file = string.Format("{0}-{1}-{2}--{3}-{4}-{5}.log", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        Directory.CreateDirectory(LogFilePath);
        sw = new StreamWriter(LogFilePath + logFilename, true, Encoding.UTF8);
    }

    private void Write(int logLevel, string format, params object[] args) {
        DateTime now = DateTime.Now;
        string timestamp = string.Format(timestampFormat, now);
        string message = string.Format(format, args);
        string prefix = GetPrefix(logLevel);
        string line = string.Format(lineFormat, timestamp, prefix, message);
        sw.WriteLine(line);
        sw.Flush();
        OnMessageLogged?.Invoke(null, new MessageLoggedEventArgs(now, logLevel, message));

        // mirror message to console
        Console.WriteLine("[{0:HH:mm:ss}] [{1}] {2}", now, prefix, message);
    }

    private void WriteError(Exception error) {
        DateTime now = DateTime.Now;
        string timestamp = string.Format(timestampFormat, now);
        string line = string.Format(lineFormat, timestamp, PrefixError, error.Message);
        sw.WriteLine(line);
        //sw.WriteLine(indent("InnerException: ") + error.InnerException);
        //sw.WriteLine(indent("Source: ") + error.Source);
        sw.WriteLine("Stacktrace: " + error.StackTrace);
        sw.Flush();
        //UnityEngine.Debug.LogError(error);
        OnMessageLogged?.Invoke(null, new MessageLoggedEventArgs(now, LevelError, error.Message));

        // mirror message to console
        Console.WriteLine("[{0:HH:mm:ss}] [{1}] {2}", now, LevelError, error.Message);
    }

    private string GetPrefix(int logLevel) {
        switch (logLevel) {
        case LevelError:
            return PrefixError;
        case LevelWarning:
            return PrefixWarning;
        case LevelInfo:
            return PrefixInfo;
        case LevelVerbose:
            return PrefixVerbose;
        case LevelDebug:
            return PrefixDebug;
        default:
            throw new ArgumentException("Invalid log level: " + logLevel);
        }
    }


    public void Dispose() {
        if (sw != null) {
            sw.WriteLine("EOF.");
            sw.Flush();
            sw.Close();
            sw.Dispose();
            sw = null;
        }
    }

    #endregion instance

}
