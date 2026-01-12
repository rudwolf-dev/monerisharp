using System.Collections.Generic;
using TorchSharp;
using MoNeriSharp.Utils;
using TorchSharp.Modules;

namespace MoNeriSharp.modules
{
    /// <summary>
    /// Interfaz común para modelos de lenguaje (LSTM, Transformer, etc.)
    /// </summary>
    public interface ILanguageModel
    {
        // Parámetros del modelo (para optimizadores)
        IEnumerable<Parameter> Parameters { get; }

        // Tamaño del vocabulario
        int VocabSize { get; }

        // Forward: recibe tokens y opcionalmente una máscara (causal, padding, etc.)
        torch.Tensor Forward(torch.Tensor tokens, torch.Tensor mask = null);

        // Generación de texto a partir de un prompt
        string Generate(string prompt,
                        Tokenizer tokenizer,
                        int maxLen = 50,
                        double temperature = 1.0,
                        int topK = 50,
                        double topP = 0.9,
                        double repetitionPenalty = 1.2);

        // Guardar y cargar modelo
        void save(string path);
        void load(string path, bool strict = true);

        // Cambiar modo entrenamiento/evaluación
        void train();
        void eval();
    }
}