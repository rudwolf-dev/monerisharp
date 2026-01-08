namespace MoNeriSharp.Data
{
    public class Sample
    {
        public string Text { get; set; } = string.Empty;
        public string LabelStr { get; set; } = string.Empty;
        public int Label { get; set; }
    }
    public static class CsvLoader
    {
        // textCol y labelCol son los índices de columna (0-based)
        public static IEnumerable<Sample> LoadCsv(
            string path,
            int textCol,
            int labelCol,
            Dictionary<string, int> labelMap)
        {
            var samples = new List<Sample>();

            using var reader = new StreamReader(path);
            string? line;
            bool headerSkipped = false;

            while ((line = reader.ReadLine()) != null)
            {
                if (!headerSkipped) { headerSkipped = true; continue; }

                // No limites a 2 columnas; usa Split completo y toma las que necesitas
                var parts = line.Split(',');
                if (parts.Length <= System.Math.Max(textCol, labelCol)) continue;

                var text = parts[textCol].Trim().Trim('"');
                var labelStr = parts[labelCol].Trim();

                samples.Add(new Sample
                {
                    Text = text,
                    LabelStr = labelStr,
                    Label = labelMap.TryGetValue(labelStr, out var idx) ? idx : 0
                });
            }

            return samples;
        }
    }
}