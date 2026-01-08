using System.Text.Json;

namespace MoNeriSharp.Utils
{
    public class Tokenizer
    {
        public Dictionary<string, int> Vocab { get; private set; }
        public Dictionary<int, string> InvVocab { get; private set; }
        public int VocabSize => Vocab.Count;

        // Índices reservados
        public const int PAD = 0;
        public const int UNK = 1;
        public const int BOS = 2;
        public const int EOS = 3;

        public Tokenizer()
        {
            Vocab = new Dictionary<string, int>();
            InitSpecialTokens();
            BuildInverse();
        }

        public Tokenizer(List<string> corpus, int vocabSize = 50000)
        {
            Vocab = new Dictionary<string, int>();
            InitSpecialTokens();
            BuildVocabulary(corpus, vocabSize);
            BuildInverse();
        }

        private void InitSpecialTokens()
        {
            Vocab["<PAD>"] = PAD;
            Vocab["<UNK>"] = UNK;
            Vocab["<BOS>"] = BOS;
            Vocab["<EOS>"] = EOS;
        }

        public void BuildVocabulary(List<string> corpus, int vocabSize = 50000)
        {
            int idx = Vocab.Count; // empieza después de los tokens especiales

            foreach (var text in corpus)
            {
                foreach (var tokenRaw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var token = tokenRaw.ToLowerInvariant();

                    if (!Vocab.ContainsKey(token))
                    {
                        Vocab[token] = idx++;
                        if (idx >= vocabSize) return;
                    }
                }
            }
            BuildInverse();
        }

        // ===== Nuevo método: expandir vocabulario =====
        public void ExpandVocabulary(List<string> corpus, int vocabSize = 50000)
        {
            int idx = Vocab.Count; // continuar desde el último índice

            foreach (var text in corpus)
            {
                foreach (var tokenRaw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var token = tokenRaw.ToLowerInvariant();

                    if (!Vocab.ContainsKey(token))
                    {
                        Vocab[token] = idx++;
                        if (idx >= vocabSize) return;
                    }
                }
            }
            BuildInverse();
        }

        private void BuildInverse()
        {
            InvVocab = new Dictionary<int, string>();
            foreach (var kv in Vocab)
                InvVocab[kv.Value] = kv.Key;
        }

        // ===== Guardar vocabulario =====
        public void Save(string path)
        {
            var json = JsonSerializer.Serialize(Vocab);
            File.WriteAllText(path, json);
        }

        // ===== Cargar vocabulario =====
        public void Load(string path)
        {
            var json = File.ReadAllText(path);
            Vocab = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            BuildInverse();
        }

        public int[] Encode(string text, int maxLen)
        {
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ids = new List<int>();

            // Añadir token de inicio
            ids.Add(BOS);

            foreach (var tokenRaw in tokens)
            {
                var token = tokenRaw.ToLowerInvariant();

                if (Vocab.TryGetValue(token, out int id))
                    ids.Add(id);
                else
                    ids.Add(UNK);
            }

            // Añadir token de fin
            ids.Add(EOS);

            // Padding
            while (ids.Count < maxLen) ids.Add(PAD);
            if (ids.Count > maxLen) ids = ids.GetRange(0, maxLen);

            return ids.ToArray();
        }

        // ===== Decode =====
        public string Decode(int[] ids)
        {
            var tokens = new List<string>();

            foreach (var id in ids)
            {
                if (id == PAD) continue;
                if (id == BOS) continue;
                if (id == EOS) break;

                if (InvVocab.TryGetValue(id, out string token))
                    tokens.Add(token);
                else
                    tokens.Add("<UNK>");
            }

            return string.Join(" ", tokens);
        }
    }
}