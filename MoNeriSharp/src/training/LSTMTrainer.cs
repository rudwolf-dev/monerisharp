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
    public static class LSTMTrainer
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
            var optimizer = torch.optim.Adam(model.Parameters, lr: lr);
            var lossFn = nn.CrossEntropyLoss(ignore_index: 0);

            double bestValLoss = double.MaxValue;
            int patienceCounter = 0;

            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                model.train();
                double trainLoss = 0;

                foreach (var batch in Batchify(trainCorpus, batchSize))
                {
                    // Convertir corpus a tensor explícito
                    var encoded = batch.Select(x => tokenizer.Encode(x, maxLenHint)).ToArray();

                    // convertir int[][] → long[,]
                    int rows = encoded.Length;
                    int cols = encoded[0].Length;
                    var rect = new long[rows, cols];
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            rect[r, c] = encoded[r][c];

                    var input = torch.tensor(rect, dtype: torch.int64);
                    var target = input.clone();

                    optimizer.zero_grad();
                    var logits = model.Forward(input);
                    var loss = lossFn.forward(logits.view(-1, tokenizer.VocabSize), target.view(-1));
                    loss.backward();
                    optimizer.step();

                    trainLoss += loss.ToSingle();
                }

                // Validación
                model.eval();
                double valLoss = 0;
                foreach (var batch in Batchify(valCorpus, batchSize))
                {
                    var encoded = batch.Select(x => tokenizer.Encode(x, maxLenHint)).ToArray();

                    // convertir int[][] → long[,]
                    int rows = encoded.Length;
                    int cols = encoded[0].Length;
                    var rect = new long[rows, cols];
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            rect[r, c] = encoded[r][c];

                    var input = torch.tensor(rect, dtype: torch.int64);
                    var target = input.clone();

                    var logits = model.Forward(input);
                    var loss = lossFn.forward(logits.view(-1, tokenizer.VocabSize), target.view(-1));
                    valLoss += loss.ToSingle();
                }

                Console.WriteLine($"Epoch {epoch}: TrainLoss={trainLoss:F4}, ValLoss={valLoss:F4}");

                if (valLoss < bestValLoss)
                {
                    bestValLoss = valLoss;
                    patienceCounter = 0;
                    model.save(Path.Combine("models", modelFileName));
                }
                else
                {
                    patienceCounter++;
                    if (patienceCounter >= patience)
                    {
                        Console.WriteLine("Early stopping activado.");
                        break;
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