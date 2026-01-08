using MoNeriSharp.Utils;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MoNeriSharp.modules
{
    public class LanguageModel : nn.Module
    {
        private Embedding embedding;
        private LSTM lstm;
        private Linear fc;

        public int VocabSize { get; }
        public int EmbedDim { get; }
        public int HiddenDim { get; }
        public int NumLayers { get; }

        public LanguageModel(string name, int vocabSize, int embedDim, int hiddenDim, int numLayers) : base(name)
        {
            VocabSize = vocabSize;
            EmbedDim = embedDim;
            HiddenDim = hiddenDim;
            NumLayers = numLayers;

            embedding = nn.Embedding(vocabSize, embedDim);
            lstm = nn.LSTM(embedDim, hiddenDim, numLayers);
            fc = nn.Linear(hiddenDim, vocabSize);

            RegisterComponents();
        }

        public Tensor Forward(Tensor tokens)
        {
            // tokens: [batch, seq_len]
            var emb = embedding.forward(tokens);              // [batch, seq_len, embedDim]
            var (output, _, _) = lstm.forward(emb);           // [batch, seq_len, hiddenDim]
            var logits = fc.forward(output);                  // [batch, seq_len, vocabSize]
            return logits;
        }

        // Generación de texto con top-k y top-p sampling
        // Generación de texto con top-k, top-p y penalización de repetición
        public string Generate(string prompt, Tokenizer tokenizer, int maxLen = 30, double temperature = 1.0, int topK = 50, double topP = 0.9, double repetitionPenalty = 1.2)
        {
            var tokens = tokenizer.Encode(prompt, maxLen: maxLen);
            var input = torch.tensor(tokens).unsqueeze(0); // batch=1

            var generated = new List<int>(tokens);

            for (int i = tokens.Length; i < maxLen; i++)
            {
                var logits = Forward(input);
                var lastStep = logits[0, i - 1]; // distribución del último token

                // aplicar penalización de repetición
                foreach (var token in generated)
                {
                    lastStep[token] /= repetitionPenalty;
                }

                // aplicar temperatura
                lastStep = lastStep / temperature;

                // convertir a probabilidades
                var probs = nn.functional.softmax(lastStep, dim: 0);

                // ordenar por probabilidad
                var sorted = probs.sort(descending: true);
                var sortedProbs = sorted.Values;
                var sortedIndices = sorted.Indices;

                // aplicar top-k
                int cutoff = Math.Min(topK, (int)sortedProbs.shape[0]);
                var topKProbs = sortedProbs[..cutoff];
                var topKIndices = sortedIndices[..cutoff];

                // aplicar top-p (nucleus sampling)
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

                // muestreo aleatorio dentro del núcleo
                var dist = torch.distributions.Categorical(probs: finalProbs);
                int sampledIndex = dist.sample().item<int>();
                var nextToken = finalIndices[sampledIndex].item<int>();

                generated.Add(nextToken);

                // actualizar input
                input = torch.tensor(generated.ToArray()).unsqueeze(0);
            }

            return tokenizer.Decode(generated.ToArray());
        }
    }
}