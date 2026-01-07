using System;
using TorchSharp;
using static TorchSharp.torch;
using MoNeriSharp.Modules;
using MoNeriSharp.Data;
using MoNeriSharp.Utils;

namespace MoNeriSharp.App
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== moNeriSharp ===");
            Console.WriteLine("Chatbot modular inicializado.");
            Console.WriteLine("Escribe tu mensaje (ENTER vacío para salir):");

            // ===== Preparar datasets =====
            var langSamples = DataLoader.LoadLangIdentify(
                @"R:\source\ia\MoNeriSharp\MoNeriSharp\data\raw\langIdentify.csv"
            );
            var emoSamples = DataLoader.LoadGeneric(
                @"R:\source\ia\MoNeriSharp\MoNeriSharp\data\raw\tEmotion.csv"
            );

            // ===== Construir vocabulario compartido =====
            var corpus = System.Linq.Enumerable.Concat(
                System.Linq.Enumerable.Select(langSamples, s => s.Text),
                System.Linq.Enumerable.Select(emoSamples, s => s.Text)
            );
            var tokenizer = new Tokenizer(corpus, minFreq: 2);

            // ===== Inicializar modelos =====
            var langClassifier = new LanguageClassifier(
                "LangClassifier",
                tokenizer.VocabSize,
                embedDim: 128,
                hiddenDim: 256,
                numLangs: 30
            );

            var emoClassifier = new EmotionClassifier(
                "EmotionClassifier",
                tokenizer.VocabSize,
                embedDim: 128,
                hiddenDim: 256,
                numEmotions: 6 // ajustar según clases reales de tEmotion.csv
            );

            var device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;
            langClassifier.to(device);
            emoClassifier.to(device);

            // ===== Loop interactivo =====
            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;

                // Tokenizar entrada
                var encoded = tokenizer.Encode(input, maxLen: 10);
                var tokens = torch.tensor(encoded, new long[] { 1, 10 }, device: device);

                // Predicciones
                var langLogits = langClassifier.Forward(tokens);
                var emoLogits = emoClassifier.Forward(tokens);

                var langId = langLogits.argmax(1).ToInt32();
                var emoId = emoLogits.argmax(1).ToInt32();

                // Pipeline simplificado
                var response = PipelineRouter.Process(input, langId, emoId);

                Console.WriteLine($"[moNeriSharp] {response}");
            }
        }
    }

    static class PipelineRouter
    {
        public static string Process(string text, int langId, int emoId)
        {
            // Paso 1: idioma
            var lang = $"LangID={langId}";

            // Paso 2: emoción
            var emotion = $"EmotionID={emoId}";

            // Paso 3: sarcasmo (dummy)
            var sarcasm = false;

            // Paso 4: machismo (dummy)
            var machismo = text.Contains("mujer") ? true : false;

            // Paso 5: rol dinámico (dummy)
            var role = "default";

            // Respuesta final
            return $"{lang}, {emotion}, Sarcasmo={sarcasm}, Machismo={machismo}, Rol={role}";
        }
    }
}