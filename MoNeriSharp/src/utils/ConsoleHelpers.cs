using System;
using System.Collections.Generic;
using System;

namespace MoNeriSharp.Utils
{
    public static class ConsoleHelpers
    {
        public static double AskDoubleWithInfo(string msg, string info, double def)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg);
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(info);
            Console.ResetColor();

            Console.Write($"Valor (default {def}): ");
            var s = Console.ReadLine();
            return string.IsNullOrWhiteSpace(s) ? def : double.TryParse(s, out var v) ? v : def;
        }

        public static void PrintHeader(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== " + text + " ===\n");
            Console.ResetColor();
        }

        public static void PrintSuccess(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void PrintWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static int AskIntWithInfo(string msg, string info, int def)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg);
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(info);
            Console.ResetColor();

            Console.Write($"Valor (default {def}): ");
            var s = Console.ReadLine();
            return string.IsNullOrWhiteSpace(s) ? def : int.TryParse(s, out var v) ? v : def;
        }

        public static bool AskBool(string msg, bool def)
        {
            Console.Write(msg);
            var s = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(s)) return def;
            return s.StartsWith("s");
        }

        public static string AskFileName(string msg, string def)
        {
            Console.Write(msg);
            var s = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(s) || s != "s") return def;

            Console.Write("Ingresa el nombre del archivo (ejemplo: miModelo.pt): ");
            var name = Console.ReadLine();
            return string.IsNullOrWhiteSpace(name) ? def : name.Trim();
        }

        public static int AskOption(string msg, string[] options, string[] descriptions, int def = 0)
        {
            Console.WriteLine(msg);
            for (int i = 0; i < options.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{i + 1}] {options[i]}");
                Console.ResetColor();
                Console.WriteLine($" → {descriptions[i]}");
            }

            Console.Write($"Selecciona opción (default {def + 1}): ");
            var s = Console.ReadLine();
            return string.IsNullOrWhiteSpace(s) ? def :
                (int.TryParse(s, out var v) && v > 0 && v <= options.Length ? v - 1 : def);
        }
    }
}
