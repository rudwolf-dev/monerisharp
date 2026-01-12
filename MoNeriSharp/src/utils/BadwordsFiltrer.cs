using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MoNeriSharp.Utils
{
    public static class BadwordFilter
    {
        // Lista de groserías inicial
        private static readonly HashSet<string> InvalidTokens = new HashSet<string>
        {
            // Español
            "puta", "puto", "mierda", "pendejo", "chingar", "chingada",
            "cabron", "pinche", "culero", "verga", "coño",
            "imbecil", "idiota", "estupido", "joto", "maricon",
            "perra", "chingon", "chingadera", "pito", "naco", "zorra",

            // Inglés
            "fuck", "shit", "bitch", "asshole", "bastard", "dick", "cunt",
            "motherfucker", "son of a bitch", "jerk", "moron", "retard",
            "slut", "whore", "hoe", "prick", "wanker"
        };

        // Patrones regex para detectar variaciones
        private static readonly List<Regex> InvalidPatterns = new List<Regex>
        {
            new Regex(@"p[\W_]*e[\W_]*n[\W_]*d[\W_]*e[\W_]*j[\W_]*o", RegexOptions.IgnoreCase),
            new Regex(@"m[\W_]*i[\W_]*e[\W_]*r[\W_]*d[\W_]*a", RegexOptions.IgnoreCase),
            new Regex(@"f[\W_]*u[\W_]*c[\W_]*k", RegexOptions.IgnoreCase),
            new Regex(@"s[\W_]*h[\W_]*i[\W_]*t", RegexOptions.IgnoreCase)
        };

        /// <summary>
        /// Limpia el corpus eliminando o reemplazando tokens ofensivos.
        /// </summary>
        public static List<string> CleanCorpus(List<string> corpus, bool replaceWithMask = true)
        {
            var cleaned = new List<string>();
            int affectedCount = 0;

            foreach (var text in corpus)
            {
                bool containsBadword = InvalidTokens.Any(w => text.ToLowerInvariant().Contains(w)) ||
                                       InvalidPatterns.Any(p => p.IsMatch(text));

                if (containsBadword)
                {
                    affectedCount++;
                    if (replaceWithMask)
                    {
                        string masked = text;
                        foreach (var word in InvalidTokens)
                            masked = Regex.Replace(masked, word, "<MASK>", RegexOptions.IgnoreCase);

                        foreach (var pattern in InvalidPatterns)
                            masked = pattern.Replace(masked, "<MASK>");

                        cleaned.Add(masked);
                    }
                    // Si no se reemplaza, se descarta la frase
                }
                else
                {
                    cleaned.Add(text);
                }
            }

            Console.WriteLine($"BadwordFilter: {affectedCount} frases afectadas.");
            return cleaned;
        }

        /// <summary>
        /// Añadir nueva palabra ofensiva.
        /// </summary>
        public static void AddInvalidToken(string token)
        {
            InvalidTokens.Add(token.ToLowerInvariant());
        }

        /// <summary>
        /// Quitar palabra ofensiva de la lista.
        /// </summary>
        public static void RemoveInvalidToken(string token)
        {
            InvalidTokens.Remove(token.ToLowerInvariant());
        }

        /// <summary>
        /// Guardar lista a JSON.
        /// </summary>
        public static void Save(string path)
        {
            var json = JsonConvert.SerializeObject(InvalidTokens.ToList(), Formatting.Indented);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Cargar lista desde JSON.
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