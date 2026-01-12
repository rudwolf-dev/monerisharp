using System;
using System.IO;

namespace MoNeriSharp
{
    public static class DirectorySetup
    {
        /// <summary>
        /// Crea todas las carpetas necesarias para el proyecto y añade un README.txt con su propósito.
        /// </summary>
        public static void EnsureProjectDirectories()
        {
            var dirsWithDescriptions = new Dictionary<string, string>
            {
                { "data", "Carpeta raíz para datasets de entrenamiento, validación y evaluación." },
                { "data/training", "Aquí van los datasets de entrenamiento (CSV/TSV)." },
                { "data/validation", "Aquí van los datasets de validación (CSV/TSV)." },
                { "data/evaluation", "Aquí van los datasets de evaluación para medir precisión del modelo." },
                { "models", "Aquí se guardan los vocabularios y checkpoints de los modelos entrenados." },
                { "logs", "Aquí se guardan los registros de entrenamiento y ejecución." }
            };

            foreach (var kvp in dirsWithDescriptions)
            {
                string dir = kvp.Key;
                string description = kvp.Value;

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Console.WriteLine($"Carpeta creada: {dir}");
                }
                else
                {
                    Console.WriteLine($"Carpeta existente: {dir}");
                }

                // Crear README.txt con descripción
                string readmePath = Path.Combine(dir, "README.txt");
                if (!File.Exists(readmePath))
                {
                    File.WriteAllText(readmePath, description);
                    Console.WriteLine($"README creado en {dir}");
                }
            }
        }
    }
}