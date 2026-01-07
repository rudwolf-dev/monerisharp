using System;
using System.Collections.Generic;
using System.IO;

namespace MoNeriSharp.Data
{
    public partial class DataLoader
    {
        /// <summary>
        /// Loader para tEmotion.csv con formato: label,text
        /// Ejemplo: alegria,"Hoy me siento muy feliz"
        /// </summary>
        public static IEnumerable<Sample> LoadEmotion(string path, Dictionary<string, int> emotionMap)
        {
            var samples = new List<Sample>();

            using (var reader = new StreamReader(path))
            {
                string? line;
                bool headerSkipped = false;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!headerSkipped)
                    {
                        headerSkipped = true;
                        continue; // saltar encabezado
                    }

                    var parts = line.Split(',', 2); // label,text
                    if (parts.Length < 2) continue;

                    var labelStr = parts[0].Trim();
                    var text = parts[1].Trim().Trim('"');

                    samples.Add(new Sample
                    {
                        Text = text,
                        LabelStr = labelStr,
                        Label = emotionMap.ContainsKey(labelStr) ? emotionMap[labelStr] : 0
                    });
                }
            }

            return samples;
        }
    }
}