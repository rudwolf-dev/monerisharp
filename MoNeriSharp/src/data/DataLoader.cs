using Microsoft.VisualBasic.FileIO;

namespace MoNeriSharp.Utils
{
    public static class DataLoader
    {
        public static void LoadEvaluation(string evalDir, out List<string> evalCorpus)
        {
            evalCorpus = new List<string>();

            var evalFiles = Directory.GetFiles(evalDir, "*.*")
                                     .Where(f => f.EndsWith(".csv") || f.EndsWith(".tsv"))
                                     .ToList();

            if (evalFiles.Count == 0)
            {
                Console.WriteLine($"No se encontraron datasets de evaluación en {evalDir}.");
                return;
            }

            Console.WriteLine($"Cargando corpus de evaluación desde {evalDir}...");
            BuildCorpusFromCsvFiles(evalFiles, evalCorpus);

            Console.WriteLine($"Corpus de evaluación cargado: {evalCorpus.Count} ejemplos");
        }

        static string SafeGet(string[] parts, int index)
        {
            if (index < 0 || index >= parts.Length) return "";
            return parts[index]?.Trim() ?? "";
        }

        public static void BuildCorpusFromCsvFiles(List<string> csvFiles, List<string> corpus)
        {
            Console.WriteLine("Cargando CSV/TSV...");

            foreach (var file in csvFiles)
            {
                using (var parser = new TextFieldParser(file))
                {
                    parser.SetDelimiters(new string[] { ",", "\t" });
                    parser.HasFieldsEnclosedInQuotes = true;

                    string[] headers = parser.ReadFields();
                    if (headers == null)
                    {
                        Console.WriteLine($"No se detectaron encabezados en {file}, se omite.");
                        continue;
                    }

                    // Detectar columnas relevantes
                    int systemCol = GetCol(headers, "system");
                    int englishCol = GetCol(headers, "English");
                    int spanishCol = GetCol(headers, "Spanish");
                    int questionCol = GetCol(headers, "Question");
                    int answerCol = GetCol(headers, "Answer");
                    int thinkCol = GetCol(headers, "think");
                    int inputsCol = GetCol(headers, "inputs");
                    int targetsCol = GetCol(headers, "targets");
                    int preguntaCol = GetCol(headers, "pregunta");
                    int respuestaCol = GetCol(headers, "respuesta");
                    int promptCol = GetCol(headers, "act");
                    int promptEsCol = GetCol(headers, "prompt_es_ES");
                    int textCol = DetectTextColumn(headers);
                    int labelCol = DetectLabelColumn(headers);

                    Console.WriteLine($"Detectadas columnas en {file}: SYS={systemCol}, EN={englishCol}, ES={spanishCol}, Q={questionCol}, A={answerCol}, THINK={thinkCol}, Inputs={inputsCol}, Targets={targetsCol}, Pregunta={preguntaCol}, Respuesta={respuestaCol}, Prompt={promptCol}, PromptEs={promptEsCol}, Text={textCol}, Label={labelCol}");

                    while (!parser.EndOfData)
                    {
                        string[] parts = parser.ReadFields();
                        if (parts == null) continue;

                        string entry = null;
                        string sysMsg = systemCol >= 0 ? $"<SYSTEM> {SafeGet(parts, systemCol)} " : "";

                        // Traducción EN-ES
                        if (spanishCol >= 0 && englishCol >= 0)
                            entry = $"{sysMsg}<USER> {SafeGet(parts, englishCol)} <SEP> {SafeGet(parts, spanishCol)} <ASSISTANT>";

                        // Inputs/targets estilo instrucción
                        else if (inputsCol >= 0 && targetsCol >= 0)
                            entry = $"{sysMsg}<SYSTEM> <INST> {SafeGet(parts, inputsCol)} </INST> <ASSISTANT> {SafeGet(parts, targetsCol)}";

                        // Pregunta/respuesta en español
                        else if (preguntaCol >= 0 && respuestaCol >= 0)
                            entry = $"{sysMsg}<USER> <QUESTION> {SafeGet(parts, preguntaCol)} <ASSISTANT> <ANSWER> {SafeGet(parts, respuestaCol)}";

                        // Pregunta/respuesta genérica con razonamiento opcional
                        else if (questionCol >= 0 && answerCol >= 0)
                        {
                            string q = SafeGet(parts, questionCol);
                            string a = SafeGet(parts, answerCol);
                            string t = thinkCol >= 0 ? SafeGet(parts, thinkCol) : null;

                            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(a))
                            {
                                entry = !string.IsNullOrWhiteSpace(t)
                                    ? $"{sysMsg}<USER> <QUESTION> {q} <THINK> {t} <ASSISTANT> <ANSWER> {a}"
                                    : $"{sysMsg}<USER> <QUESTION> {q} <ASSISTANT> <ANSWER> {a}";
                            }
                        }

                        // Prompt estilo instrucción
                        else if (promptCol >= 0 && promptEsCol >= 0)
                            entry = $"<SYSTEM> {SafeGet(parts, promptCol)} <INST> {SafeGet(parts, promptEsCol)} </INST>";

                        // Texto libre con etiqueta (clasificación)
                        else if (textCol >= 0)
                        {
                            string text = SafeGet(parts, textCol);
                            string label = labelCol >= 0 ? SafeGet(parts, labelCol) : null;
                            if (!string.IsNullOrWhiteSpace(text))
                                entry = string.IsNullOrWhiteSpace(label)
                                    ? $"{sysMsg}<USER> {text}"
                                    : $"{sysMsg}<USER> {text} <SEP> {label} <ASSISTANT>";
                        }

                        if (!string.IsNullOrWhiteSpace(entry))
                            corpus.Add(entry);
                    }
                }
            }
        }

        public static void LoadTrainAndValidation(string trainDir, string valDir, out List<string> trainCorpus, out List<string> valCorpus)
        {
            trainCorpus = new List<string>();
            valCorpus = new List<string>();

            var trainFiles = Directory.GetFiles(trainDir, "*.*")
                                      .Where(f => f.EndsWith(".csv") || f.EndsWith(".tsv"))
                                      .ToList();

            var valFiles = Directory.GetFiles(valDir, "*.*")
                                    .Where(f => f.EndsWith(".csv") || f.EndsWith(".tsv"))
                                    .ToList();

            Console.WriteLine($"Cargando corpus de entrenamiento desde {trainDir}...");
            BuildCorpusFromCsvFiles(trainFiles, trainCorpus);

            if (valFiles.Count > 0)
            {
                Console.WriteLine($"Cargando corpus de validación desde {valDir}...");
                BuildCorpusFromCsvFiles(valFiles, valCorpus);
            }
            else
            {
                Console.WriteLine("No se encontraron datasets de validación. Se dividirá el corpus de entrenamiento (80/20 aleatorio).");

                var rnd = new Random();
                trainCorpus = trainCorpus.OrderBy(_ => rnd.Next()).ToList();

                int valCount = (int)(trainCorpus.Count * 0.2);
                valCorpus = trainCorpus.Take(valCount).ToList();
                trainCorpus = trainCorpus.Skip(valCount).ToList();
            }

            Console.WriteLine($"Corpus cargado: Train={trainCorpus.Count} | Val={valCorpus.Count}");
        }

        static int GetCol(string[] headers, string name) =>
            Array.FindIndex(headers, h => h.Equals(name, StringComparison.OrdinalIgnoreCase));

        static int DetectTextColumn(string[] headers)
        {
            string[] candidates = { "text", "texto", "content", "sentence", "sentencia", "prompt", "inputs", "review", "line" };
            foreach (var c in candidates)
            {
                int idx = GetCol(headers, c);
                if (idx >= 0) return idx;
            }
            return headers.Length > 0 ? 0 : -1;
        }

        static int DetectLabelColumn(string[] headers)
        {
            string[] candidates = { "label", "etiqueta", "class", "categoria", "sarcasmo", "score", "targets" };
            foreach (var c in candidates)
            {
                int idx = GetCol(headers, c);
                if (idx >= 0) return idx;
            }
            return -1;
        }
    }
}