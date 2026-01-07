using MoNeriSharp.Data;
using MoNeriSharp.Modules;
using MoNeriSharp.Utils;
using System;
using System.Collections.Generic;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace MoNeriSharp.Training
{
    public class EmotionTrainer
    {
        public static void Train(string dataPath, int epochs = 5, int batchSize = 32)
        {
            // 1. Cargar dataset
            var samples = DataLoader.LoadGeneric(dataPath); // text, label
            var corpus = samples.Select(s => s.Text);
            var tokenizer = new Tokenizer(corpus, minFreq: 2);

            // 2. Inicializar modelo
            var model = new EmotionClassifier("EmotionClassifier", tokenizer.VocabSize, 128, 256, numEmotions: 6);
            var device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;
            model.to(device);

            // 3. Optimizador
            var optim = torch.optim.Adam(model.parameters(), lr: 3e-4);

            // 4. Loop de entrenamiento
            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                int batchCount = 0;
                double totalLoss = 0;

                foreach (var batch in Batch(samples, batchSize))
                {
                    var encoded = batch.Select(s => tokenizer.Encode(s.Text, maxLen: 10)).ToArray();
                    var flat = encoded.SelectMany(x => x).ToArray();
                    var tokens = torch.tensor(flat, new long[] { batch.Count, 10 }, device: device);

                    var labels = torch.tensor(batch.Select(s => (long)s.Label).ToArray(), device: device);

                    var logits = model.Forward(tokens);
                    var loss = torch.nn.functional.cross_entropy(logits, labels);

                    optim.zero_grad();
                    loss.backward();
                    optim.step();

                    totalLoss += loss.ToSingle();
                    batchCount++;
                }

                Console.WriteLine($"[Emotion] Epoch {epoch}/{epochs} - Loss promedio: {totalLoss / batchCount:F4}");
            }
        }

        private static IEnumerable<List<DataLoader.Sample>> Batch(IEnumerable<DataLoader.Sample> samples, int batchSize)
        {
            var batch = new List<DataLoader.Sample>();
            foreach (var s in samples)
            {
                batch.Add(s);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<DataLoader.Sample>();
                }
            }
            if (batch.Count > 0) yield return batch;
        }
    }
}