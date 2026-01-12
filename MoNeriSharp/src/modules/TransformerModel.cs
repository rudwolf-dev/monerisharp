using MoNeriSharp.Utils;
using System.Collections.Generic;
using System.Linq;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MoNeriSharp.modules
{
    public class TransformerModel : nn.Module, ILanguageModel
    {
        private Embedding tokenEmbedding;
        private Embedding positionEmbedding;
        private ModuleList<TransformerBlock> layers;
        private Linear fc;
        private LayerNorm norm;

        public int VocabSize { get; }
        public int EmbedDim { get; }
        public int NumHeads { get; }
        public int NumLayers { get; }
        public int MaxSeqLen { get; }

        public IEnumerable<Parameter> Parameters => base.parameters();

        public TransformerModel(string name, int vocabSize, int embedDim, int numHeads, int numLayers, int maxSeqLen, torch.Device device = null)
            : base(name)
        {
            VocabSize = vocabSize;
            EmbedDim = embedDim;
            NumHeads = numHeads;
            NumLayers = numLayers;
            MaxSeqLen = maxSeqLen;

            if (device == null) device = torch.CPU;

            // 🔑 Embeddings de tokens y posiciones aprendibles
            tokenEmbedding = nn.Embedding(vocabSize, embedDim, device: device);
            positionEmbedding = nn.Embedding(maxSeqLen, embedDim, device: device);

            // Bloques Transformer GPT‑like
            layers = nn.ModuleList(
                Enumerable.Range(0, numLayers)
                    .Select(i => new TransformerBlock($"block{i}", embedDim, numHeads, device))
                    .ToArray()
            );

            // Normalización final + capa de salida
            norm = nn.LayerNorm(embedDim, device: device);
            fc = nn.Linear(embedDim, vocabSize, device: device);

            RegisterComponents();

            torch.nn.init.xavier_uniform_(tokenEmbedding.weight);
            torch.nn.init.xavier_uniform_(positionEmbedding.weight);
            torch.nn.init.xavier_uniform_(fc.weight);
        }

        // 🔑 Forward con máscara causal
        public torch.Tensor Forward(torch.Tensor tokens, torch.Tensor mask = null)
        {
            var batchSize = tokens.size(0);
            var seqLen = tokens.size(1);

            var positions = torch.arange(0, seqLen, dtype: torch.int64, device: tokens.device)
                                  .unsqueeze(0)
                                  .expand(batchSize, seqLen);

            var x = tokenEmbedding.forward(tokens) + positionEmbedding.forward(positions);

            foreach (var block in layers)
                x = block.Forward(x, mask);

            x = norm.forward(x);
            var logits = fc.forward(x);
            return logits;
        }

        public string Generate(string prompt, Tokenizer tokenizer, int maxLen = 50,
                               double temperature = 1.0, int topK = 50, double topP = 0.9, double repetitionPenalty = 1.2)
        {
            this.eval();

            var tokens = tokenizer.Encode(prompt, maxLen: maxLen);
            var input = torch.tensor(tokens, dtype: torch.int64).unsqueeze(0);
            var generated = new List<int>(tokens);

            for (int i = tokens.Length; i < maxLen; i++)
            {
                var seqLen = input.size(1);
                var mask = torch.tril(torch.ones(new long[] { seqLen, seqLen }, dtype: ScalarType.Bool));

                var logits = Forward(input, mask);
                var lastStep = logits[0, i - 1];

                foreach (var token in generated)
                    lastStep[token] /= repetitionPenalty;

                lastStep = lastStep / temperature;
                var probs = nn.functional.softmax(lastStep, dim: 0);

                var sorted = probs.sort(descending: true);
                var sortedProbs = sorted.Values;
                var sortedIndices = sorted.Indices;

                int cutoff = Math.Min(topK, (int)sortedProbs.shape[0]);
                var topKProbs = sortedProbs[..cutoff];
                var topKIndices = sortedIndices[..cutoff];

                double cumulative = 0;
                int nucleusCount = 0;
                for (int j = 0; j < cutoff; j++)
                {
                    cumulative += topKProbs[j].ToSingle();
                    nucleusCount++;
                    if (cumulative >= topP) break;
                }

                var finalProbs = topKProbs[..nucleusCount];
                var finalIndices = topKIndices[..nucleusCount];

                var dist = torch.distributions.Categorical(probs: finalProbs);
                int sampledIndex = dist.sample().item<int>();
                var nextToken = finalIndices[sampledIndex].item<int>();

                generated.Add(nextToken);
                input = torch.tensor(generated.ToArray(), dtype: torch.int64).unsqueeze(0);

                if (nextToken == 1) break; // <EOS>
            }

            var decoded = tokenizer.Decode(generated.ToArray());
            int idxAnswer = decoded.IndexOf("<ANSWER>");
            if (idxAnswer >= 0)
                decoded = decoded.Substring(idxAnswer + "<ANSWER>".Length).Trim();

            return decoded;
        }

        public void save(string path) => base.save(path);
        public void load(string path, bool strict = true) => base.load(path, strict);
        public void train() => base.train();
        public void eval() => base.eval();
    }
}