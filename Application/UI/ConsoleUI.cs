using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UI
{
    public static class ConsoleUI
    {
        public static void Clear()
        {
            Console.Clear();
        }

        public static void Header(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine($" {title.ToUpper()}");
            Console.WriteLine("========================================");
            Console.ResetColor();
        }

        public static void Option(string key, string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" [{key}] ");
            Console.ResetColor();

            Console.WriteLine(text);
        }

        public static void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[WARN] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ResetColor();
            Console.ReadKey(true);
        }
    }
}
