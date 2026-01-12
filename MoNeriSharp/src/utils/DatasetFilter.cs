using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MoNeriSharp.Utils
{
    public static class DatasetFilter
    {
        private static HashSet<string> CensoredCategories = new HashSet<string>();
        private static List<Regex> CensoredPatterns = new List<Regex>();

        /// <summary>
        /// Cargar categorías censuradas desde un CSV (columna 5 = category_title).
        /// </summary>
        public static void LoadCsv(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"No se encontró dataset en {filePath}");
                return;
            }

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines.Skip(1)) // saltar encabezado
            {
                var parts = line.Split(',');
                if (parts.Length > 4)
                {
                    string category = parts[4].Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(category))
                        CensoredCategories.Add(category);
                }
            }

            Console.WriteLine($"DatasetFilter: {CensoredCategories.Count} categorías censuradas cargadas desde CSV.");
        }

        /// <summary>
        /// Guardar categorías censuradas a JSON.
        /// </summary>
        public static void SaveJson(string path)
        {
            var json = JsonConvert.SerializeObject(CensoredCategories.ToList(), Formatting.Indented);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Cargar categorías censuradas desde JSON.
        /// </summary>
        public static void LoadJson(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var list = JsonConvert.DeserializeObject<List<string>>(json);
                if (list != null)
                {
                    CensoredCategories.Clear();
                    foreach (var cat in list)
                        CensoredCategories.Add(cat.ToLowerInvariant());
                }
            }
        }

        /// <summary>
        /// Añadir patrón regex de censura.
        /// </summary>
        public static void AddPattern(string pattern)
        {
            CensoredPatterns.Add(new Regex(pattern, RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Limpia corpus eliminando frases que contengan categorías censuradas.
        /// </summary>
        public static List<string> CleanCorpus(List<string> corpus)
        {
            var cleaned = new List<string>();
            int removedCount = 0;

            foreach (var text in corpus)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                string normalized = text;

                bool censored = CensoredCategories.Any(cat => normalized.Contains(cat)) ||
                                CensoredPatterns.Any(p => p.IsMatch(normalized));

                if (censored)
                {
                    removedCount++;
                    continue;
                }

                cleaned.Add(text.Trim());
            }

            Console.WriteLine($"DatasetFilter: {removedCount} frases eliminadas por categorías censuradas.");
            return cleaned;
        }
    }
}