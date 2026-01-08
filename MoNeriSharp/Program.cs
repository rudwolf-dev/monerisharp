using MoNeriSharp.modules;
using MoNeriSharp.Training;
using MoNeriSharp.Utils;
using ParquetSharp;
using System.Data;
using TorchSharp;
using static TorchSharp.torch;

using MoNeriSharp.Utils;
using MoNeriSharp.modules;
using MoNeriSharp.Training;
using TorchSharp;
using static TorchSharp.torch;

namespace MoNeriSharp.App
{
    class Program
    {
        static void Main(string[] args)
        {
            DatasetFilter.LoadCsv("data/censura.csv");

            Console.WriteLine("Entrenando moNeriLM con LanguageTrainer...");

            // ===== Preguntar parámetros =====
            Console.Write("👉 Ingresa número de epochs (default 2): ");
            string epochsInput = Console.ReadLine();
            int epochs = string.IsNullOrWhiteSpace(epochsInput) ? 2 : int.Parse(epochsInput);

            Console.Write("👉 Ingresa tamaño de batch (default 32): ");
            string batchInput = Console.ReadLine();
            int batchSize = string.IsNullOrWhiteSpace(batchInput) ? 32 : int.Parse(batchInput);

            Console.Write("👉 ¿Quieres filtrar groserías? (s/n, default s): ");
            string filterInput = Console.ReadLine();
            bool filtrarGroserias = string.IsNullOrWhiteSpace(filterInput) || filterInput.Trim().ToLower() == "s";

            Console.Write("👉 ¿Quieres guardar con otro nombre? (s/n, default n): ");
            string saveNameInput = Console.ReadLine();
            string modelFileName = "moNeriLM.pt";
            if (!string.IsNullOrWhiteSpace(saveNameInput) && saveNameInput.Trim().ToLower() == "s")
            {
                Console.Write("👉 Ingresa el nombre del archivo (ejemplo: miModelo.pt): ");
                string customName = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(customName))
                    modelFileName = customName.Trim();
            }

            var device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;
            string dataDir = "data/raw";

            // ===== Buscar archivos por prioridad =====
            var parquetFiles = Directory.GetFiles(dataDir, "*.parquet").ToList();
            var ptFiles = Directory.GetFiles(dataDir, "*.pt").ToList();
            var csvFiles = Directory.GetFiles(dataDir, "*.*")
                                    .Where(f => f.EndsWith(".csv") || f.EndsWith(".tsv"))
                                    .ToList();

            var corpus = new List<string>();
            var labels = new List<string>();

            if (parquetFiles.Any())
            {
                Console.WriteLine("✅ Se encontraron datasets .parquet, se cargan con máxima prioridad...");
                foreach (var parquetFile in parquetFiles)
                {
                    using (var fileReader = new ParquetFileReader(parquetFile))
                    {
                        for (int i = 0; i < fileReader.FileMetaData.NumRowGroups; i++)
                        {
                            using (var rgReader = fileReader.RowGroup(i))
                            {
                                for (int j = 0; j < rgReader.MetaData.NumColumns; j++)
                                {
                                    using (var colReader = rgReader.Column(j).LogicalReader<string>())
                                    {
                                        var values = colReader.ReadAll((int)rgReader.MetaData.NumRows);
                                        foreach (var val in values)
                                        {
                                            if (!string.IsNullOrWhiteSpace(val))
                                                corpus.Add(val.ToLowerInvariant());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (ptFiles.Any())
            {
                Console.WriteLine("✅ Se encontraron datasets .pt, se cargan con prioridad...");
                foreach (var ptFile in ptFiles)
                {
                    var obj = torch.load(ptFile);
                    if (obj is Tensor tensor)
                    {
                        Console.WriteLine($"Cargando tensor desde {ptFile}, shape: {string.Join("x", tensor.shape)}");
                        var arr = tensor.data<float>();
                        foreach (var val in arr)
                        {
                            corpus.Add(val.ToString().ToLowerInvariant());
                        }
                    }
                    else
                    {
                        Console.WriteLine($"El archivo {ptFile} no es un tensor directo, revisar contenido.");
                    }
                }
            }
            else
            {
                Console.WriteLine("⚡ No se encontraron .parquet ni .pt, se cargan CSV/TSV...");
                foreach (var file in csvFiles)
                {
                    var lines = File.ReadAllLines(file);
                    var headers = lines[0].Split(new[] { ',', '\t' });

                    int textCol = DetectTextColumn(headers);
                    int labelCol = DetectLabelColumn(headers);

                    if (textCol < 0)
                    {
                        Console.WriteLine($"No se detectó columna de texto en {file}, se omite.");
                        continue;
                    }

                    foreach (var line in lines.Skip(1))
                    {
                        var parts = line.Split(new[] { ',', '\t' });
                        if (textCol < parts.Length)
                        {
                            var text = parts[textCol].Trim().ToLowerInvariant();
                            string label = "";

                            if (labelCol >= 0 && labelCol < parts.Length)
                            {
                                label = parts[labelCol].Trim().ToLowerInvariant();
                            }

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                if (!string.IsNullOrWhiteSpace(label))
                                {
                                    corpus.Add($"{text} <SEP> {label}");
                                    labels.Add(label);
                                }
                                else
                                {
                                    corpus.Add(text);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Corpus total: {corpus.Count} frases");
            Console.WriteLine($"Etiquetas detectadas: {labels.Count}");

            // ===== Tokenizer expansivo =====
            Directory.CreateDirectory("models");
            string vocabPath = Path.Combine("models", "vocab.json");

            var tokenizer = new Tokenizer();

            if (File.Exists(vocabPath))
            {
                Console.WriteLine("✅ Vocabulario encontrado, cargando...");
                tokenizer.Load(vocabPath);

                Console.WriteLine("⚡ Expandiendo vocabulario con nuevas palabras del corpus...");
                corpus = TokenFilter.CleanCorpus(corpus);
                if (filtrarGroserias) corpus = BadwordFilter.CleanCorpus(corpus);
                corpus = DatasetFilter.CleanCorpus(corpus);

                tokenizer.ExpandVocabulary(corpus, vocabSize: 50000);
                tokenizer.Save(vocabPath);
            }
            else
            {
                Console.WriteLine("⚡ No se encontró vocabulario, construyendo uno nuevo...");
                corpus = TokenFilter.CleanCorpus(corpus);
                if (filtrarGroserias) corpus = BadwordFilter.CleanCorpus(corpus);
                corpus = DatasetFilter.CleanCorpus(corpus);

                tokenizer.ExpandVocabulary(corpus, vocabSize: 50000);
                tokenizer.Save(vocabPath);
            }

            // ===== Modelo de lenguaje =====
            string modelPath = Path.Combine("models", modelFileName);
            LanguageModel model;

            if (File.Exists(modelPath))
            {
                Console.WriteLine("✅ Modelo encontrado, cargando checkpoint...");
                model = new LanguageModel("moNeriLM", tokenizer.VocabSize, embedDim: 128, hiddenDim: 256, numLayers: 2);
                model.load(modelPath);
            }
            else
            {
                Console.WriteLine("⚡ No se encontró modelo, creando uno nuevo...");
                model = new LanguageModel("moNeriLM", tokenizer.VocabSize, embedDim: 128, hiddenDim: 256, numLayers: 2);
            }

            model.to(device);

            // ===== Entrenamiento con LanguageTrainer =====
            LanguageTrainer.Train(
                model,
                corpus,
                tokenizer,
                epochs: epochs,
                batchSize: batchSize,
                maxLen: 20,
                lr: 3e-4,
                validationSplit: 0.1,
                patience: 3
            );

            Console.Write("Escribe tu mensaje:");
            string prompt = Console.ReadLine();

            string user = "Usuario";
            string response = model.Generate($"{user}: {prompt} <SEP>", tokenizer, maxLen: 50, temperature: 0.9, topK: 40, topP: 0.9, repetitionPenalty: 1.2);
            Console.WriteLine($"\nMoNeriLM: {response}");

            // ===== Guardar modelo =====
            model.save(modelPath);
            Console.WriteLine($"\nEntrenamiento finalizado. Modelo guardado en {modelPath}");
        }

        
        // ===== Funciones auxiliares que se deben mantener =====
        static int DetectTextColumn(string[] headers)
        {
            return Array.FindIndex(headers, h =>
                h.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Texto", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("sentence", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("sentence1", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("sentence2", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("answer", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("prompt", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("prompt_es_ES", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("English", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Spanish", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Frase Antonima", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Contexto", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Pregunta", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Respuesta", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("title", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("ingredients", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("steps", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("think", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("response", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("word", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("synonyms", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("situation", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("texto_machista", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("texto_inclusivo", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("contexto", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("explicacion", StringComparison.OrdinalIgnoreCase)
            );
        }

        static int DetectLabelColumn(string[] headers)
        {
            return Array.FindIndex(headers, h =>
                h.Equals("label", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("labels", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("score", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Sarcasmo", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("nivel", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("subcategoria", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("pais", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("emotion", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Number", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Continente", StringComparison.OrdinalIgnoreCase));
        }
    }
}