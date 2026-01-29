using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.HelperUI
{
    public static class UIHelper
    {
        public static void DrawHeader(string naslov)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            // boja konzole 
            Console.WriteLine("===========================================");
            Console.WriteLine($"   {naslov}");
            Console.WriteLine("===========================================");
            Console.ResetColor();
        }
        public static void DrawOpition(string kljuc , string tekst)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{kljuc}]");
            Console.ResetColor();
            Console.WriteLine(tekst);

        }
        public static void DrawInfo(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ✔ " + text);
            Console.ResetColor();
        }

        public static void DrawError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ✖ " + text);
            Console.ResetColor();
        }

        public static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\nPritisni ENTER za nastavak...");
            Console.ResetColor();
            Console.ReadLine();
        }
    }
}
