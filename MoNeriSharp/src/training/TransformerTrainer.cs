using MoNeriSharp.modules;
using MoNeriSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MoNeriSharp.Training
{
    public static class TransformerTrainer
    {
        public static void Train(ILanguageModel model,
                                 List<string> trainCorpus,
                                 List<string> valCorpus,
                                 Tokenizer tokenizer,
                                 int epochs,
                                 int batchSize,
                                 int maxLenHint,
                                 double lr,
                                 int patience,
                                 string modelFileName)
        {
            // 🔑 AdamW en lugar de Adam
            var optimizer = torch.optim.AdamW(model.Parameters, lr: lr, weight_decay: 0.01);
            var lossFn = nn.CrossEntropyLoss(ignore_index: 0);

            double bestPerplexity = double.MaxValue;
            int patienceCounter = 0;

            string metricsFile = Path.Combine("logs", "training_metrics.csv");
            using (var writer = new StreamWriter(metricsFile))
            {
                writer.WriteLine("Epoch,TrainLoss,ValLoss,TrainAcc,ValAcc,Perplexity");

                for (int epoch = 1; epoch <= epochs; epoch++)
                {
                    model.train();
                    double trainLoss = 0;
                    double trainCorrect = 0;
                    double trainTotal = 0;

                    int totalTrainBatches = Batchify(trainCorpus, batchSize).Count();
                    int trainBatchNum = 0;

                    foreach (var batch in Batchify(trainCorpus, batchSize))
                    {
                        trainBatchNum++;
                        var encoded = batch.Select(x => tokenizer.Encode(x, maxLenHint)).ToArray();
                        int rows = encoded.Length;
                        int cols = encoded[0].Length;
                        var rect = new long[rows, cols];
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                                rect[r, c] = encoded[r][c];

                        var input = torch.tensor(rect, dtype: torch.int64);
                        var target = input.clone();

                        // 🔑 Causal mask triangular inferior
                        var seqLen = input.size(1);
                        var mask = torch.tril(torch.ones(new long[] { seqLen, seqLen }, dtype: ScalarType.Bool));

                        optimizer.zero_grad();
                        var logits = model.Forward(input, mask); // ahora acepta máscara
                        var loss = lossFn.forward(logits.view(-1, tokenizer.VocabSize), target.view(-1));
                        loss.backward();
                        optimizer.step();

                        trainLoss += loss.ToSingle();
                        var predictions = logits.argmax(-1);
                        trainCorrect += predictions.eq(target).sum().ToSingle();
                        trainTotal += target.numel();

                        Console.WriteLine($"[Train] Epoch {epoch} Batch {trainBatchNum}/{totalTrainBatches}: Loss={loss.ToSingle():F4}");
                    }

                    // Validación
                    model.eval();
                    double valLoss = 0;
                    double valCorrect = 0;
                    double valTotal = 0;
                    int totalValBatches = Batchify(valCorpus, batchSize).Count();

                    foreach (var batch in Batchify(valCorpus, batchSize))
                    {
                        var encoded = batch.Select(x => tokenizer.Encode(x, maxLenHint)).ToArray();
                        int rows = encoded.Length;
                        int cols = encoded[0].Length;
                        var rect = new long[rows, cols];
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                                rect[r, c] = encoded[r][c];

                        var input = torch.tensor(rect, dtype: torch.int64);
                        var target = input.clone();

                        var seqLen = input.size(1);
                        var mask = torch.tril(torch.ones(new long[] { seqLen, seqLen }, dtype: ScalarType.Bool));

                        var logits = model.Forward(input, mask);
                        var loss = lossFn.forward(logits.view(-1, tokenizer.VocabSize), target.view(-1));
                        valLoss += loss.ToSingle();

                        var predictions = logits.argmax(-1);
                        valCorrect += predictions.eq(target).sum().ToSingle();
                        valTotal += target.numel();
                    }

                    double avgTrainLoss = trainLoss / totalTrainBatches;
                    double avgValLoss = valLoss / totalValBatches;
                    double trainAccuracy = trainCorrect / trainTotal;
                    double valAccuracy = valCorrect / valTotal;
                    double perplexity = Math.Exp(avgValLoss);

                    Console.WriteLine($"Epoch {epoch}: TrainLoss={avgTrainLoss:F4}, ValLoss={avgValLoss:F4}, TrainAcc={trainAccuracy:P2}, ValAcc={valAccuracy:P2}, Perplexity={perplexity:F2}");
                    writer.WriteLine($"{epoch},{avgTrainLoss:F4},{avgValLoss:F4},{trainAccuracy:F4},{valAccuracy:F4},{perplexity:F2}");

                    if (perplexity < bestPerplexity)
                    {
                        bestPerplexity = perplexity;
                        patienceCounter = 0;
                        model.save(Path.Combine("models", modelFileName));
                    }
                    else
                    {
                        patienceCounter++;
                        if (patienceCounter >= patience)
                        {
                            Console.WriteLine("Early stopping activado (criterio: perplexity).");
                            break;
                        }
                    }
                }
            }
        }

        private static IEnumerable<List<string>> Batchify(List<string> corpus, int batchSize)
        {
            for (int i = 0; i < corpus.Count; i += batchSize)
                yield return corpus.Skip(i).Take(batchSize).ToList();
        }
    }
}