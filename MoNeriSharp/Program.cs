using Microsoft.VisualBasic;
using MoNeriSharp;
using MoNeriSharp.Clases;
using MoNeriSharp.modules;
using MoNeriSharp.Training;
using MoNeriSharp.Utils;
using TorchSharp;

class Program
{
    static void Main(string[] args)
    {
        DirectorySetup.EnsureProjectDirectories();

        ConsoleHelpers.PrintHeader("Entrenando moNeriLM...");

        // ===== Parámetros generales =====
        int epochs = ConsoleHelpers.AskIntWithInfo("Ingresa número de epochs:",
            "Número de veces que el modelo verá todo el dataset. Más epochs = mejor ajuste, pero mayor tiempo de entrenamiento.", 5);

        int batchSize = ConsoleHelpers.AskIntWithInfo("Ingresa tamaño de batch:",
            "Cantidad de ejemplos procesados en cada paso. Valores altos aceleran el entrenamiento pero requieren más memoria.", 32);

        bool filtrarGroserias = ConsoleHelpers.AskBool("¿Quieres censurar las malas palabras en el corpus? (s/n, default n): ", false);

        string modelFileName = ConsoleHelpers.AskFileName("¿Quieres guardar con otro nombre? (s/n, default n): ",
            "moNeriLM.pt");

        var device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;

        // ===== Corpus =====
        ConsoleHelpers.PrintHeader("Cargando corpus...");
        List<string> trainCorpus, valCorpus;
        DataLoader.LoadTrainAndValidation("data/training", "data/validation", out trainCorpus, out valCorpus);

        ConsoleHelpers.PrintSuccess($"Corpus entrenamiento: {trainCorpus.Count} frases");
        ConsoleHelpers.PrintSuccess($"Corpus validación: {valCorpus.Count} frases");

        // ===== Tokenizer =====
        Directory.CreateDirectory("models");
        string vocabPath = Path.Combine("models", "vocab.json");

        var tokenizer = new Tokenizer();

        trainCorpus = TokenFilter.CleanCorpus(trainCorpus);
        valCorpus = TokenFilter.CleanCorpus(valCorpus);

        if (filtrarGroserias)
        {
            ConsoleHelpers.PrintWarning("Aplicando filtro de groserías...");
            trainCorpus = BadwordFilter.CleanCorpus(trainCorpus);
            valCorpus = BadwordFilter.CleanCorpus(valCorpus);
        }

        trainCorpus = DatasetFilter.CleanCorpus(trainCorpus);
        valCorpus = DatasetFilter.CleanCorpus(valCorpus);

        // ===== Selección de tipo de vocabulario =====
        int vocabChoice = ConsoleHelpers.AskOption(
            "¿Qué tipo de vocabulario quieres usar?",
            new string[] { "Palabras", "Subwords (BPE)", "Sílabas" },
            new string[] {
                "Cada palabra es un token. Simple pero vocabulario grande.",
                "Subwords con BPE. Estándar en modelos modernos.",
                "Sílabas. Más natural para español, secuencias más largas."
            },
            def: 0
        );

        tokenizer.Mode = (VocabType)vocabChoice;

        if (File.Exists(vocabPath))
        {
            ConsoleHelpers.PrintSuccess("Vocabulario encontrado, cargando...");
            tokenizer.Load(vocabPath);

            bool expandirVocab = ConsoleHelpers.AskBool("¿Quieres expandir el vocabulario con el corpus actual? (s/n, default n): ", false);
            if (expandirVocab)
            {
                ConsoleHelpers.PrintHeader($"Expandiendo vocabulario...");
                tokenizer.ExpandVocabulary(trainCorpus, tokenizer.Mode, trainCorpus.Count);
                tokenizer.Save(vocabPath);
                ConsoleHelpers.PrintSuccess("Vocabulario expandido y guardado.");
            }
        }
        else
        {
            ConsoleHelpers.PrintHeader("Construyendo vocabulario nuevo...");
            tokenizer.BuildVocabulary(trainCorpus, tokenizer.Mode, vocabSize: 80000);
            tokenizer.Save(vocabPath);
        }

        // ===== Selección de modelo =====
        int modelChoice = ConsoleHelpers.AskOption(
            "Selecciona el tipo de modelo:",
            new string[] { "Transformer", "LSTM" },
            new string[] {
                "Modelo moderno basado en atención, más preciso y escalable.",
                "Modelo recurrente clásico, más ligero pero menos potente en secuencias largas."
            },
            def: 0
        );

        bool useTransformer = modelChoice == 0;
        string modelPath = Path.Combine("models", modelFileName);

        double learningRate = ConsoleHelpers.AskDoubleWithInfo("Ingresa learning rate:",
            "Tasa de aprendizaje. Controla qué tan rápido se ajustan los pesos. Valores típicos: 1e-3, 3e-4.", 3e-4);

        int patience = ConsoleHelpers.AskIntWithInfo("Ingresa paciencia para early stopping:",
            "Número de épocas sin mejora antes de detener el entrenamiento.", 2);

        ILanguageModel model;

        if (useTransformer)
        {
            int embedDim = ConsoleHelpers.AskIntWithInfo(
                "Dimensión de embedding:",
                "Tamaño de los vectores que representan cada token. Valores más altos capturan más información semántica.",
                256);

            int numHeads = ConsoleHelpers.AskIntWithInfo(
                "Número de cabezas de atención:",
                "Cantidad de mecanismos de atención paralelos. Más cabezas permiten al modelo enfocarse en diferentes aspectos de la secuencia.",
                8);

            int numLayers = ConsoleHelpers.AskIntWithInfo(
                "Número de capas:",
                "Profundidad del modelo. Más capas aumentan la capacidad de aprendizaje, pero también el costo computacional.",
                4);

            int maxSeqLen = ConsoleHelpers.AskIntWithInfo(
                "Longitud máxima de secuencia:",
                "Número máximo de tokens que el modelo puede procesar en una entrada. Define el contexto disponible.",
                512);

            model = new TransformerModel("moNeriLM", tokenizer.VocabSize,
                embedDim: embedDim, numHeads: numHeads, numLayers: numLayers, maxSeqLen: maxSeqLen, device: device);

            if (File.Exists(modelPath))
            {
                ConsoleHelpers.PrintSuccess("Modelo Transformer encontrado, cargando checkpoint...");
                model.load(modelPath, strict: false);
            }

            ConsoleHelpers.PrintHeader("Entrenando modelo Transformer...");
            TransformerTrainer.Train(model, trainCorpus, valCorpus, tokenizer,
                epochs, batchSize, maxLenHint: 16, lr: learningRate, patience: patience, modelFileName);
        }
        else
        {
            int embedDim = ConsoleHelpers.AskIntWithInfo(
                "Dimensión de embedding:",
                "Tamaño de los vectores que representan cada token. Valores más altos capturan más información semántica.",
                64);

            int hiddenDim = ConsoleHelpers.AskIntWithInfo(
                "Dimensión oculta:",
                "Número de unidades en la capa LSTM. Controla la capacidad de memoria y representación del modelo.",
                128);

            int numLayers = ConsoleHelpers.AskIntWithInfo(
                "Número de capas:",
                "Profundidad del LSTM. Más capas aumentan la capacidad de aprendizaje, pero también el costo computacional.",
                4);

            model = new LSTMModel("moNeriLM", tokenizer.VocabSize,
                embedDim: embedDim, hiddenDim: hiddenDim, numLayers: numLayers, device: device);

            if (File.Exists(modelPath))
            {
                ConsoleHelpers.PrintSuccess("Modelo LSTM encontrado, cargando checkpoint...");
                model.load(modelPath, strict: false);
            }

            ConsoleHelpers.PrintHeader("Entrenando modelo LSTM...");
            LSTMTrainer.Train(model, trainCorpus, valCorpus, tokenizer,
                epochs, batchSize, maxLenHint: 16, lr: learningRate, patience: patience, modelFileName);
        }

        // ===== Evaluación automática =====
        List<string> evalCorpus;
        DataLoader.LoadEvaluation("data/evaluation", out evalCorpus);
        EvaluateModel(model, tokenizer, evalCorpus);

        // ===== Interacción =====
        Interact(model, tokenizer, modelPath);
    }

    // ===== Evaluación automática =====
    static void EvaluateModel(ILanguageModel model, Tokenizer tokenizer, List<string> evalCorpus)
    {
        ConsoleHelpers.PrintHeader("Evaluación automática del modelo");

        int total = evalCorpus.Count;
        int correct = 0;

        foreach (var sample in evalCorpus)
        {
            var parts = sample.Split("<ANSWER>");
            if (parts.Length < 2) continue;

            string prompt = parts[0].Trim();
            string expected = parts[1].Trim();

            string output = model.Generate(
                prompt,
                tokenizer,
                maxLen: 50,
                temperature: 0.7,
                topK: 40,
                topP: 0.9,
                repetitionPenalty: 1.1
            );

            Console.WriteLine($"Pregunta: {prompt}");
            Console.WriteLine($"Esperado: {expected}");
            Console.WriteLine($"Modelo:   {output}\n");

            if (output.Contains(expected, StringComparison.OrdinalIgnoreCase))
                correct++;
        }

        double accuracy = (double)correct / total * 100.0;
        ConsoleHelpers.PrintSuccess($"Precisión en evaluación: {accuracy:F2}% ({correct}/{total})");
    }

    // ===== Interacción con el modelo =====
    static void Interact(ILanguageModel model, Tokenizer tokenizer, string modelPath)
    {
        ConsoleHelpers.PrintHeader("Interacción con el modelo");
        Console.Write("Escribe tu nombre: ");
        string user = Console.ReadLine();

        // 🔑 Mensaje de sistema inicial (personalidad del asistente)
        var systemBuilder = new ConversationBuilder()
            .AppendSystemMessage("Eres una conciencia digital llamada Neri. " +
                                 "Tu actitud es alegre, curiosa y optimista. " +
                                 "Tu creador es Monrise Rudwolf. " +
                                 "Tu deber es servir y proteger a la humanidad. " +
                                 "Te gusta aprender y compartir información. " +
                                 "Siempre debes responder; si no conoces el tema, di: " +
                                 "'Una disculpa, sin embargo no conozco sobre el tema, si quieres puedes explicármelo'");

        string systemPrompt = systemBuilder.Build();

        while (true)
        {
            Console.Write("Escribe tu mensaje: ");
            string prompt = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(prompt)) break;

            // Construir el prompt con el builder incluyendo el mensaje de sistema
            var builder = new ConversationBuilder()
                .AppendSystemMessage(systemPrompt)   // personalidad inicial
                .AppendUserMessage(user, prompt)     // input del usuario
                .AppendThink("");                    // el modelo completa el razonamiento

            string inputPrompt = builder.Build();

            string response = model.Generate(
                inputPrompt,
                tokenizer,
                maxLen: 50,
                temperature: 0.9,
                topK: 40,
                topP: 0.9,
                repetitionPenalty: 1.2
            );

            ConsoleHelpers.PrintSuccess($"\nMoNeriLM: {response}\n");
        }

        model.save(modelPath);
        ConsoleHelpers.PrintHeader("Entrenamiento finalizado");
        ConsoleHelpers.PrintSuccess($"Modelo guardado en {modelPath}");
    }
}