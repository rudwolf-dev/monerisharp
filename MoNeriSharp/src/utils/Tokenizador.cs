using System;
using System.Collections.Generic;
using System.Linq;

namespace MoNeriSharp.Utils
{
    /// <summary>
    /// Tokenizador simple basado en vocabulario de palabras.
    /// Convierte texto en índices enteros para TorchSharp.
    /// </summary>
    public class Tokenizer
    {
        private Dictionary<string, int> vocab;
        private int unkId;
        private int padId;

        public Tokenizer(IEnumerable<string> corpus, int minFreq = 1)
        {
            vocab = new Dictionary<string, int>();
            var freq = new Dictionary<string, int>();

            // Contar frecuencia de palabras
            foreach (var text in corpus)
            {
                foreach (var word in text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!freq.ContainsKey(word)) freq[word] = 0;
                    freq[word]++;
                }
            }

            // IDs reservados
            padId = 0;
            unkId = 1;
            vocab["<PAD>"] = padId;
            vocab["<UNK>"] = unkId;

            int idx = 2;
            foreach (var kv in freq.Where(kv => kv.Value >= minFreq))
            {
                vocab[kv.Key] = idx++;
            }
        }

        public int[] Encode(string text, int maxLen = 10)
        {
            var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tokens = new List<int>();

            foreach (var w in words)
            {
                if (vocab.ContainsKey(w))
                    tokens.Add(vocab[w]);
                else
                    tokens.Add(unkId);
            }

            // Padding / truncado
            if (tokens.Count < maxLen)
            {
                tokens.AddRange(Enumerable.Repeat(padId, maxLen - tokens.Count));
            }
            else if (tokens.Count > maxLen)
            {
                tokens = tokens.Take(maxLen).ToList();
            }

            return tokens.ToArray();
        }

        public string Decode(int[] tokens)
        {
            var inv = vocab.ToDictionary(kv => kv.Value, kv => kv.Key);
            return string.Join(" ", tokens.Select(t => inv.ContainsKey(t) ? inv[t] : "<UNK>"));
        }

        public int VocabSize => vocab.Count;
    }
}