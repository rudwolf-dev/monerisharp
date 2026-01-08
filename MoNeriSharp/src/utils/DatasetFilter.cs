public static class DatasetFilter
{
    private static HashSet<string> CensoredCategories = new HashSet<string>();

    public static void LoadCsv(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"⚠ No se encontró dataset en {filePath}");
            return;
        }

        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines.Skip(1)) // saltar encabezado
        {
            var parts = line.Split(',');
            if (parts.Length > 4)
            {
                string category = parts[4].Trim().ToLowerInvariant(); // category_title
                if (!string.IsNullOrWhiteSpace(category))
                    CensoredCategories.Add(category);
            }
        }

        Console.WriteLine($"✅ Dataset cargado con {CensoredCategories.Count} categorías censuradas");
    }

    public static List<string> CleanCorpus(List<string> corpus)
    {
        var cleaned = new List<string>();
        foreach (var text in corpus)
        {
            if (string.IsNullOrWhiteSpace(text)) continue;
            string normalized = text.ToLowerInvariant();

            // Si contiene alguna categoría censurada, se descarta
            if (CensoredCategories.Any(cat => normalized.Contains(cat)))
                continue;

            cleaned.Add(normalized);
        }
        return cleaned;
    }
}