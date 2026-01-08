using MoNeriSharp.Data;
using MoNeriSharp.Utils;
using TorchSharp;
using static TorchSharp.torch;

namespace MoNeriSharp.Training
{
    public static class UniversalTrainer
    {
        public static void TrainSingleTask(
            nn.Module model,
            IEnumerable<Sample> corpus,
            Tokenizer tokenizer,
            int epochs = 5,
            int batchSize = 32,
            int maxLen = 10,
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
                // Barajar samples
                var rnd = new Random();
                trainSet = trainSet.OrderBy(_ => rnd.Next()).ToList();

                double totalLoss = 0;
                int batches = 0;
                int correct = 0;
                int total = 0;

                foreach (var batch in Batch(trainSet, batchSize))
                {
                    var enc = batch.Select(s => tokenizer.Encode(s.Text, maxLen)).ToArray();
                    var flat = enc.SelectMany(x => x).ToArray();
                    var tokens = torch.tensor(flat, new long[] { batch.Count, maxLen }, device: device);

                    var logits = ((dynamic)model).Forward(tokens);

                    var labels = torch.tensor(
                        batch.Select(s => s.Label).ToArray(),
                        new long[] { batch.Count },
                        device: device,
                        dtype: ScalarType.Int64
                    );

                    var loss = torch.nn.functional.cross_entropy(logits, labels);

                    optim.zero_grad();
                    loss.backward();
                    optim.step();

                    totalLoss += loss.item<float>();
                    batches++;

                    // Accuracy
                    var preds = logits.argmax(1);
                    correct += (preds == labels).sum().item<int>();
                    total += batch.Count;
                }

                double trainLoss = totalLoss / batches;
                double trainAcc = (double)correct / total;

                // Validación
                double valLoss = 0;
                int valCorrect = 0;
                int valTotal = 0;
                foreach (var batch in Batch(valSet, batchSize))
                {
                    var enc = batch.Select(s => tokenizer.Encode(s.Text, maxLen)).ToArray();
                    var flat = enc.SelectMany(x => x).ToArray();
                    var tokens = torch.tensor(flat, new long[] { batch.Count, maxLen }, device: device);

                    var logits = ((dynamic)model).Forward(tokens);

                    var labels = torch.tensor(
                        batch.Select(s => s.Label).ToArray(),
                        new long[] { batch.Count },
                        device: device,
                        dtype: ScalarType.Int64
                    );

                    var loss = torch.nn.functional.cross_entropy(logits, labels);
                    valLoss += loss.item<float>();

                    var preds = logits.argmax(1);
                    valCorrect += (preds == labels).sum().item<int>();
                    valTotal += batch.Count;
                }

                valLoss /= Math.Max(1, valSet.Count / batchSize);
                double valAcc = (double)valCorrect / Math.Max(1, valTotal);

                Console.WriteLine($"[Train] Epoch {epoch}/{epochs} - Loss: {trainLoss:F4} - Acc: {trainAcc:P2} | ValLoss: {valLoss:F4} - ValAcc: {valAcc:P2}");

                // Early stopping
                if (valLoss < bestValLoss)
                {
                    bestValLoss = valLoss;
                    epochsNoImprovement = 0;
                    model.save($"models/best_model_epoch{epoch}.pt");
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

        private static IEnumerable<List<Sample>> Batch(List<Sample> items, int size)
        {
            for (int i = 0; i < items.Count; i += size)
                yield return items.GetRange(i, Math.Min(size, items.Count - i));
        }
    }
}