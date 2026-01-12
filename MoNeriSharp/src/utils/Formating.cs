using System;
using System.Collections.Generic;
using System.Text;

namespace MoNeriSharp.Utils
{
    public static class Format
    {
        // Tokens especiales como strings
        public const string SPACE = "<SPACE>";
        public const string NEWLINE = "<NEWLINE>";

        /// <summary>
        /// Normaliza texto y lo convierte en una secuencia de tokens,
        /// preservando espacios, saltos de línea y puntuación.
        /// </summary>
        public static IEnumerable<string> TokenizePreservingFormat(string line)
        {
            if (line == null) yield break;

            // Normalizar saltos
            line = line.Replace("\r\n", "\n").Replace("\r", "\n");

            foreach (char c in line)
            {
                if (c == ' ')
                {
                    yield return SPACE;
                }
                else if (c == '\n')
                {
                    yield return NEWLINE;
                }
                else
                {
                    // Puntuación y símbolos se devuelven como string
                    yield return c.ToString();
                }
            }
        }

        /// <summary>
        /// Reconstruye texto desde tokens, respetando espacios y saltos.
        /// </summary>
        public static string Detokenize(IEnumerable<string> tokens)
        {
            var sb = new StringBuilder();
            foreach (var tok in tokens)
            {
                if (tok == SPACE) sb.Append(' ');
                else if (tok == NEWLINE) sb.Append('\n');
                else sb.Append(tok);
            }
            return sb.ToString();
        }
    }
}