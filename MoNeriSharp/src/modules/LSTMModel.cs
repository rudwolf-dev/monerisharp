using MoNeriSharp.Utils;
using System.Collections.Generic;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MoNeriSharp.modules
{
    public class LSTMModel : nn.Module, ILanguageModel
    {
        private Embedding embedding;
        private LSTM lstm;
        private Dropout dropout;
        private Linear fc;
        private SpecialToken SpecialToken = new SpecialToken();

        public int VocabSize { get; }
        public int EmbedDim { get; }
        public int HiddenDim { get; }
        public int NumLayers { get; }

        // Exponer parámetros para el optimizador
        public IEnumerable<Parameter> Parameters => base.parameters();

        public LSTMModel(string name, int vocabSize, int embedDim, int hiddenDim, int numLayers, torch.Device device = null)
            : base(name)
        {
            VocabSize = vocabSize;
            EmbedDim = embedDim;
            HiddenDim = hiddenDim;
            NumLayers = numLayers;

            if (device == null) device = torch.CPU;

            embedding = nn.Embedding(vocabSize, embedDim, device: device);
            lstm = nn.LSTM(embedDim, hiddenDim, numLayers, batchFirst: true, device: device);
            dropout = nn.Dropout(0.3);
            fc = nn.Linear(hiddenDim, vocabSize, device: device);

            RegisterComponents();

            torch.nn.init.xavier_uniform_(embedding.weight);
            torch.nn.init.xavier_uniform_(fc.weight);
        }

        // 🔑 Forward con firma extendida (ignora mask)
        public torch.Tensor Forward(torch.Tensor tokens, torch.Tensor mask = null)
        {
            var emb = embedding.forward(tokens);
            var (output, _, _) = lstm.forward(emb);
            output = dropout.forward(output);
            var logits = fc.forward(output);
            return logits;
        }

        public string Generate(string prompt, Tokenizer tokenizer, int maxLen = 30,
                               double temperature = 1.0, int topK = 0, double topP = 0.0, double repetitionPenalty = 1.2)
        {
            this.eval(); // desactivar dropout
            var tokens = tokenizer.Encode(prompt, maxLen: maxLen);
            var input = torch.tensor(tokens, dtype: torch.int64).unsqueeze(0);
            var generated = new List<int>(tokens);

            var h = torch.zeros(NumLayers, 1, HiddenDim);
            var c = torch.zeros(NumLayers, 1, HiddenDim);

            for (int i = tokens.Length; i < maxLen; i++)
            {
                var emb = embedding.forward(input[0, i - 1].unsqueeze(0).unsqueeze(0));
                var (output, hNew, cNew) = lstm.forward(emb, (h, c));
                h = hNew; c = cNew;

                var lastStep = fc.forward(output.squeeze(0).squeeze(0));
                var probs = nn.functional.softmax(lastStep / temperature, dim: 0);

                int nextToken;
                if (topK > 0)
                {
                    var sorted = probs.sort(descending: true);
                    var topKIndices = sorted.Indices[..topK];
                    var topKProbs = sorted.Values[..topK];
                    var dist = torch.distributions.Categorical(probs: topKProbs);
                    var sampledIndex = dist.sample().item<int>();
                    nextToken = topKIndices[sampledIndex].item<int>();
                }
                else
                {
                    nextToken = torch.multinomial(probs, 1).item<int>();
                }

                generated.Add(nextToken);
                input = torch.tensor(new int[] { nextToken }, dtype: torch.int64).unsqueeze(0).unsqueeze(0);

                if (nextToken == 1) break; // <EOS>
            }

            var decoded = tokenizer.Decode(generated.ToArray());
            int idxAnswer = decoded.IndexOf("<ANSWER>");
            if (idxAnswer >= 0)
                decoded = decoded.Substring(idxAnswer + "<ANSWER>".Length).Trim();

            return decoded;
        }

        // Implementación de ILanguageModel
        public void save(string path) => base.save(path);
        public void load(string path, bool strict = true) => base.load(path, strict);

        public void train() => base.train();
        public void eval() => base.eval();
    }
}