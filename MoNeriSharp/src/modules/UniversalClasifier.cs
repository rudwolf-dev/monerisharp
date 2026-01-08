using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MoNeriSharp.modules
{
    // ===== Clase TextClassifier =====
    public class TextClassifier : nn.Module
    {
        private Embedding embedding;
        private LSTM lstm;
        private Linear fc;

        public TextClassifier(string name, int vocabSize, int embedDim, int hiddenDim, int numLangs)
            : base(name)
        {
            embedding = nn.Embedding(vocabSize, embedDim);
            lstm = nn.LSTM(embedDim, hiddenDim, numLayers: 1, batchFirst: true);
            fc = nn.Linear(hiddenDim, numLangs);

            RegisterComponents();
        }

        public Tensor Forward(Tensor tokens)
        {
            // tokens: [batch, seq_len]
            var emb = embedding.forward(tokens); // [batch, seq_len, embedDim]
            var (output, _, _) = lstm.forward(emb); // [batch, seq_len, hiddenDim]
            var pooled = output.mean(new long[] { 1 }); // mean pooling sobre secuencia
            var logits = fc.forward(pooled);     // [batch, numLangs]
            return logits;
        }
    }
}
