using IceBloc.Utility;
using System;
using System.IO;

namespace IceBloc.Utility;

public class Output
{
    public static void WriteLine(string message, MessageType type)
    {
        Console.ForegroundColor = ConsoleColor.White;
        switch (type)
        {
            case MessageType.Error:
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.Write("[ERROR]     ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(" " + message);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case MessageType.Warning:
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.Write("[WARN]      ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" " + message);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case MessageType.Info:
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.Write("[INFO]      ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" " + message);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case MessageType.Debug:
                if (Settings.Debug)
                {
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("[DEBUG]     ");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine(" " + message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                break;
            case MessageType.Success:
                Console.BackgroundColor = ConsoleColor.Green;
                Console.Write("[SUCCESS]   ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" " + message);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case MessageType.ErrorQuit:
                WriteLine(message, MessageType.Error);
                WriteLog(MessageType.Error, message);
                WriteLine("Terminated.", MessageType.Info);
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadLine();
                Environment.Exit(1);
                break;
            case MessageType:
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.Write("[STACKTRACE]");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(" " + message);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            default:
                WriteLine("Something tried to create an error message without a valid error type!", MessageType.Error);
                break;
        }
    }

    public static void WriteLine(string message)
    {
        WriteLine(message, MessageType.Info);
    }

    public static void WriteLine(int errorCode)
    {
        var trace = new System.Diagnostics.StackTrace();
        WriteLine($"StarkEngine threw an error @ {trace.GetFrame(1).GetMethod().Name}", MessageType.Error);
        WriteLine(Error.KeyValuePairs[errorCode], MessageType.Error);
        var frames = trace.GetFrames();
        for (int i = 1; i < 6; i++)
        {
            WriteLine($"Ln {frames[i].GetFileLineNumber()}:Ch {frames[i].GetFileColumnNumber()}", MessageType.StackTrace);
            WriteLine($"\t{frames[i].GetMethod().Name}", MessageType.StackTrace);
        }
        WriteLine("Error was not caught, StarkEngine must halt.", MessageType.ErrorQuit);
    }

    /// <summary>
    /// Writes an exception message to the log.
    /// </summary>
    public static void WriteLog(Exception e)
    {
        // Writes all exception details to a file.
        string path = AppDomain.CurrentDomain.BaseDirectory + "/Output.log";
        using (StreamWriter writer = new(path, true))
        {
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), $"Exception caught in IceBloc {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), $"[Error]: {e.Message}");
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "\n" + e.StackTrace);
        }
        WriteLine($"Error \"{e}\"has been logged.", MessageType.Info);
    }

    public static void WriteLog(MessageType type, params string[] values)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + "/Output.log";
        using StreamWriter writer = new(path, true);
        foreach (var message in values)
        {
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), $"[{type}] {message}");
        }
    }

    public enum MessageType
    {
        Error,
        Warning,
        Info,
        Debug,
        Success,
        ErrorQuit,
        StackTrace
    }
}
