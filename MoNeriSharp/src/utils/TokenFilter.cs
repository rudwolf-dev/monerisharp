using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MoNeriSharp.Utils
{
    public static class TokenFilter
    {
        // Lista de palabras erróneas, raras o en otro idioma
        private static readonly HashSet<string> InvalidTokens = new HashSet<string>
        {
            // Errores comunes / ruido
            "asdfgh", "qwerty", "errortoken", "palabra_rara",
            "undefined", "null", "nan", "12345", "!!!", "???",
            "test", "demo", "lorem", "ipsum", "xxx", "zzz", "aaaa",
            "11111", "00000", "wtf", "lol", "rofl",

            // Otros idiomas (ejemplo)
            "bonjour", "ciao", "hallo", "こんにちは", "привет",
            "salut", "merci", "au revoir", "grazie", "arrivederci",
            "tschüss", "danke", "さようなら", "ありがとう",
            "до свидания", "спасибо", "olá", "tchau", "obrigado",
        };

        // Patrones regex para detectar ruido
        private static readonly List<Regex> InvalidPatterns = new List<Regex>
        {
            new Regex(@"^\d+$"),          // solo números
            new Regex(@"^[!?]+$"),        // solo signos
            new Regex(@"^[a-z]{1,2}$"),   // tokens demasiado cortos (ej. "a", "b")
        };

        /// <summary>
        /// Limpia el corpus eliminando tokens inválidos.
        /// </summary>
        public static List<string> CleanCorpus(List<string> corpus)
        {
            var cleaned = new List<string>();
            int removedCount = 0;

            foreach (var text in corpus)
            {
                var filteredTokens = text
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token =>
                        !InvalidTokens.Contains(token.ToLowerInvariant()) &&
                        !InvalidPatterns.Any(p => p.IsMatch(token))
                    );

                if (filteredTokens.Any())
                {
                    cleaned.Add(string.Join(" ", filteredTokens));
                }
                else
                {
                    removedCount++;
                }
            }

            Console.WriteLine($"TokenFilter: {removedCount} frases eliminadas por contener solo tokens inválidos.");
            return cleaned;
        }

        /// <summary>
        /// Permite añadir nuevas palabras inválidas dinámicamente.
        /// </summary>
        public static void AddInvalidToken(string token)
        {
            InvalidTokens.Add(token.ToLowerInvariant());
        }

        /// <summary>
        /// Permite quitar palabras inválidas de la lista.
        /// </summary>
        public static void RemoveInvalidToken(string token)
        {
            InvalidTokens.Remove(token.ToLowerInvariant());
        }

        /// <summary>
        /// Guardar lista de tokens inválidos a JSON.
        /// </summary>
        public static void Save(string path)
        {
            var json = JsonConvert.SerializeObject(InvalidTokens.ToList(), Formatting.Indented);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Cargar lista de tokens inválidos desde JSON.
        /// </summary>
        public static void Load(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var list = JsonConvert.DeserializeObject<List<string>>(json);
                if (list != null)
                {
                    InvalidTokens.Clear();
                    foreach (var token in list)
                        InvalidTokens.Add(token.ToLowerInvariant());
                }
            }
        }
    }
}