using MoNeriSharp.Utils;
using TorchSharp;
using static TorchSharp.torch;

namespace MoNeriSharp.Training
{
    public static class LanguageTrainer
    {
        public static void Train(
            nn.Module model,
            List<string> corpus,
            Tokenizer tokenizer,
            int epochs = 5,
            int batchSize = 32,
            int maxLen = 20,
            double lr = 3e-4,
            double validationSplit = 0.1,
            int patience = 3)
        {
            var device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;
            model.to(device);
            var optim = torch.optim.Adam(model.parameters(), lr: lr);

            var list = corpus.ToList();

            // Separar train y validación
            int valCount = (int)(list.Count * validationSplit);
            var valSet = list.Take(valCount).ToList();
            var trainSet = list.Skip(valCount).ToList();

            double bestValLoss = double.MaxValue;
            int epochsNoImprovement = 0;

            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                // Barajar corpus
                var rnd = new Random();
                trainSet = trainSet.OrderBy(_ => rnd.Next()).ToList();

                double totalLoss = 0;
                int batches = 0;

                foreach (var batch in Batch(trainSet, batchSize))
                {
                    // Encode cada frase → tensor [batch, maxLen]
                    var enc = batch.Select(s => tokenizer.Encode(s, maxLen)).ToArray();
                    var flat = enc.SelectMany(x => x).ToArray();
                    var tokens = torch.tensor(flat, new long[] { batch.Count, maxLen }, device: device, dtype: ScalarType.Int64);

                    // Inputs = todos menos el último token
                    var inputs = tokens.narrow(1, 0, maxLen - 1);
                    // Targets = todos menos el primero
                    var targets = tokens.narrow(1, 1, maxLen - 1);

                    optim.zero_grad();

                    // Forward → logits [batch, seqLen, vocabSize]
                    var logits = ((dynamic)model).Forward(inputs);       // [batch, seqLen, vocabSize]
                    var reshaped = logits.reshape(-1, ((dynamic)model).VocabSize); // [batch*seqLen, vocabSize]
                    var targetFlat = targets.reshape(-1).to_type(ScalarType.Int64); // [batch*seqLen]
                    var loss = torch.nn.functional.cross_entropy(reshaped, targetFlat);
                    loss.backward();
                    optim.step();

                    totalLoss += loss.item<float>();
                    batches++;
                    Console.WriteLine($"Epoch {epoch} | Batch {batches} | Loss: {loss.item<float>():F4}");
                }

                double trainLoss = totalLoss / Math.Max(1, batches);

                // Validación
                double valLoss = 0;
                int valBatches = 0;
                foreach (var batch in Batch(valSet, batchSize))
                {
                    var enc = batch.Select(s => tokenizer.Encode(s, maxLen)).ToArray();
                    var flat = enc.SelectMany(x => x).ToArray();
                    var tokens = torch.tensor(flat, new long[] { batch.Count, maxLen }, device: device, dtype: ScalarType.Int64);

                    var inputs = tokens.narrow(1, 0, maxLen - 1);
                    var targets = tokens.narrow(1, 1, maxLen - 1);

                    var logits = ((dynamic)model).Forward(inputs);

                    var reshaped = logits.reshape(-1, ((dynamic)model).VocabSize);
                    var targetFlat = targets.reshape(-1).to_type(ScalarType.Int64);

                    var loss = torch.nn.functional.cross_entropy(reshaped, targetFlat);
                    valLoss += loss.item<float>();
                    valBatches++;
                    Console.WriteLine($"[Train] Epoch {epoch}/{epochs} - Loss: {trainLoss:F4}");
                }

                valLoss /= Math.Max(1, valBatches);

                Console.WriteLine($"[Train] Epoch {epoch}/{epochs} - Loss: {trainLoss:F4} | ValLoss: {valLoss:F4}");

                // Early stopping
                if (valLoss < bestValLoss)
                {
                    bestValLoss = valLoss;
                    epochsNoImprovement = 0;
                    model.save($"models/best_lm_epoch{epoch}.pt");
                }
                else
                {
                    epochsNoImprovement++;
                    if (epochsNoImprovement >= patience)
                    {
                        Console.WriteLine($"Early stopping at epoch {epoch} (no improvement in {patience} epochs).");
                        break;
                    }
                }
            }
        }

        private static IEnumerable<List<string>> Batch(List<string> items, int size)
        {
            for (int i = 0; i < items.Count; i += size)
                yield return items.GetRange(i, Math.Min(size, items.Count - i));
        }
    }
}