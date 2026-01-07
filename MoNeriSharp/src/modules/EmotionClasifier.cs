using System;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace MoNeriSharp.Modules
{
    /// <summary>
    /// Clasificador de emociones con TorchSharp.
    /// Similar al LanguageClassifier pero con salida multiclass.
    /// </summary>
    public class EmotionClassifier : Module
    {
        private Embedding embedding;
        private LSTM lstm;
        private Linear fc;

        public EmotionClassifier(string name, int vocabSize, int embedDim, int hiddenDim, int numEmotions)
            : base(name)
        {
            embedding = nn.Embedding(vocabSize, embedDim);
            lstm = nn.LSTM(embedDim, hiddenDim, numLayers: 1, batchFirst: true);
            fc = nn.Linear(hiddenDim, numEmotions);

            RegisterComponents();
        }

        public Tensor Forward(Tensor tokens)
        {
            var emb = embedding.forward(tokens);
            var (output, _, _) = lstm.forward(emb);
            var pooled = output.mean(new long[] { 1 }); // mean pooling sobre secuencia
            var logits = fc.forward(pooled);
            return logits;
        }
    }
}