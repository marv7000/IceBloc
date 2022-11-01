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
                break;
            case MessageType.Warning:
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.Write("[WARN]      ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" " + message);
                break;
            case MessageType.Info:
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.Write("[INFO]      ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" " + message);
                break;
            case MessageType.Debug:
                if (Settings.Debug)
                {
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("[DEBUG]     ");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine(" " + message);
                }
                break;
            case MessageType.Success:
                Console.BackgroundColor = ConsoleColor.Green;
                Console.Write("[SUCCESS]   ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" " + message);
                break;
            case MessageType.ErrorQuit:
                WriteLine(message, MessageType.Error);
                WriteLog(MessageType.Error, message);
                WriteLine("Terminated", MessageType.Info);
                Console.ReadLine();
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

    /// <summary>
    /// Writes an <see cref="Exception"/> to the log.
    /// </summary>
    public static void WriteLog(Exception e)
    {
        Console.WriteLine();
        // Writes all exception details to a file.
        string path = AppDomain.CurrentDomain.BaseDirectory + "\\Output.log";
        using (StreamWriter writer = new(path, true))
        {
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), $"Exception caught in IceBloc {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), $"[Error]: {e.Message}");
            writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "\n" + e.StackTrace);
        }
        WriteLine("Error has been logged.", MessageType.Success);
    }

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="type">Type of the Message.</param>
    /// <param name="values">All lines of messages to write.</param>
    public static void WriteLog(MessageType type, params string[] values)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + "\\Output.log";
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
        ErrorQuit
    }
}
