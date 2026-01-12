using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MoNeriSharp.Utils
{
    // ===== Tokens especiales =====
    public enum SpecialToken
    {
        PAD = 0,
        EOS,
        UNK,
        ANSWER,
        USER,
        ASSISTANT,
        SYSTEM,
        SEP,
        START,
        END,
        MASK,
        QUESTION,
        CONTEXT,
        TITLE,
        SUMMARY,
        DATA,
        CODE,
        URL,
        DATE,
        NUMBER,
        EXCLAMATION_OPEN,
        EXCLAMATION_CLOSE,
        QUESTION_OPEN,
        QUESTION_CLOSE,
        PAREN_OPEN,
        PAREN_CLOSE,
        BRACE_OPEN,
        BRACE_CLOSE,
        BRACKET_OPEN,
        BRACKET_CLOSE,
        ANGLE_OPEN,
        ANGLE_CLOSE,
        SPACE,      // <SPACE>
        NEWLINE     // <NEWLINE>
    }

    // ===== Tipos de vocabulario =====
    public enum VocabType
    {
        Word,
        Subword,
        Syllable
    }

    public class Tokenizer
    {
        public Dictionary<string, int> Vocab { get; private set; }
        public Dictionary<int, string> VocabInverse { get; private set; }
        public int VocabSize => Vocab.Count;

        public bool CaseSensitive { get; set; } = true;
        public VocabType Mode { get; set; } = VocabType.Word;

        // Subword vocabulario y merges
        private Dictionary<string, int> SubwordVocab;
        private List<(string, string)> Merges;

        public Tokenizer()
        {
            Vocab = new Dictionary<string, int>();

            // Inicializar todos los tokens especiales desde el enum
            foreach (SpecialToken token in Enum.GetValues(typeof(SpecialToken)))
            {
                Vocab.Add($"<{token}>", (int)token);
            }

            VocabInverse = Vocab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        // ===== Construcción de vocabulario =====
        public void BuildVocabulary(IEnumerable<string> corpus, VocabType vocabType, int vocabSize = 50000)
        {
            switch (Mode)
            {
                case VocabType.Word:
                    Vocabulary.BuildWordVocabulary(Vocab, VocabInverse, corpus, CaseSensitive, vocabSize);
                    break;
                case VocabType.Subword:
                    Vocabulary.BuildSubwordVocabulary(out SubwordVocab, out Merges, corpus, vocabSize);
                    break;
                case VocabType.Syllable:
                    Vocabulary.BuildSyllableVocabulary(Vocab, VocabInverse, corpus, CaseSensitive, vocabSize);
                    break;
            }
        }

        // ===== Expansión de vocabulario =====
        public void ExpandVocabulary(IEnumerable<string> corpus, VocabType vocabType, int maxNewTokens)
        {
            switch (Mode)
            {
                case VocabType.Word:
                    Vocabulary.ExpandWordVocabulary(Vocab, VocabInverse, corpus, CaseSensitive, maxNewTokens);
                    break;
                case VocabType.Subword:
                    Vocabulary.BuildSubwordVocabulary(out SubwordVocab, out Merges, corpus, Vocab.Count + maxNewTokens);
                    break;
                case VocabType.Syllable:
                    Vocabulary.ExpandSyllableVocabulary(Vocab, VocabInverse, corpus, CaseSensitive, maxNewTokens);
                    break;
            }
        }

        // ===== Encode =====
        public int[] Encode(string text, int maxLen = 50)
        {
            if (!CaseSensitive)
                text = text.ToLowerInvariant();

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            var tokens = new List<int>();

            foreach (char c in text)
            {
                // Espacios y saltos
                if (c == ' ') { tokens.Add((int)SpecialToken.SPACE); continue; }
                if (c == '\n') { tokens.Add((int)SpecialToken.NEWLINE); continue; }

                // Puntuación y símbolos explícitos
                switch (c)
                {
                    case '¡': tokens.Add((int)SpecialToken.EXCLAMATION_OPEN); continue;
                    case '!': tokens.Add((int)SpecialToken.EXCLAMATION_CLOSE); continue;
                    case '¿': tokens.Add((int)SpecialToken.QUESTION_OPEN); continue;
                    case '?': tokens.Add((int)SpecialToken.QUESTION_CLOSE); continue;
                    case '(': tokens.Add((int)SpecialToken.PAREN_OPEN); continue;
                    case ')': tokens.Add((int)SpecialToken.PAREN_CLOSE); continue;
                    case '{': tokens.Add((int)SpecialToken.BRACE_OPEN); continue;
                    case '}': tokens.Add((int)SpecialToken.BRACE_CLOSE); continue;
                    case '[': tokens.Add((int)SpecialToken.BRACKET_OPEN); continue;
                    case ']': tokens.Add((int)SpecialToken.BRACKET_CLOSE); continue;
                    case '<': tokens.Add((int)SpecialToken.ANGLE_OPEN); continue;
                    case '>': tokens.Add((int)SpecialToken.ANGLE_CLOSE); continue;
                }

                var symbol = c.ToString();

                switch (Mode)
                {
                    case VocabType.Syllable:
                        var syllables = Silabificador.SplitIntoSyllables(symbol);
                        foreach (var s in syllables)
                            tokens.Add(Vocab.ContainsKey(s) ? Vocab[s] : (int)SpecialToken.UNK);
                        break;

                    case VocabType.Subword:
                        if (SubwordVocab != null)
                        {
                            var symbols = symbol.ToCharArray().Select(ch => ch.ToString()).ToList();
                            foreach (var merge in Merges)
                            {
                                for (int i = 0; i < symbols.Count - 1; i++)
                                {
                                    if (symbols[i] == merge.Item1 && symbols[i + 1] == merge.Item2)
                                    {
                                        symbols[i] = symbols[i] + symbols[i + 1];
                                        symbols.RemoveAt(i + 1);
                                        i = Math.Max(i - 1, 0);
                                    }
                                }
                            }
                            foreach (var s in symbols)
                                tokens.Add(SubwordVocab.ContainsKey(s) ? SubwordVocab[s] : (int)SpecialToken.UNK);
                        }
                        break;

                    default: // Word
                        tokens.Add(Vocab.ContainsKey(symbol) ? Vocab[symbol] : (int)SpecialToken.UNK);
                        break;
                }
            }

            while (tokens.Count < maxLen) tokens.Add((int)SpecialToken.PAD);
            if (tokens.Count > maxLen) tokens = tokens.Take(maxLen).ToList();

            return tokens.ToArray();
        }

        // ===== Decode =====
        public string Decode(int[] tokens)
        {
            var sb = new StringBuilder();
            foreach (var t in tokens)
            {
                if (t == (int)SpecialToken.PAD || t == (int)SpecialToken.EOS) continue;

                if (t == (int)SpecialToken.SPACE) { sb.Append(' '); continue; }
                if (t == (int)SpecialToken.NEWLINE) { sb.Append('\n'); continue; }

                if (t == (int)SpecialToken.EXCLAMATION_OPEN) { sb.Append('¡'); continue; }
                if (t == (int)SpecialToken.EXCLAMATION_CLOSE) { sb.Append('!'); continue; }
                if (t == (int)SpecialToken.QUESTION_OPEN) { sb.Append('¿'); continue; }
                if (t == (int)SpecialToken.QUESTION_CLOSE) { sb.Append('?'); continue; }
                if (t == (int)SpecialToken.PAREN_OPEN) { sb.Append('('); continue; }
                if (t == (int)SpecialToken.PAREN_CLOSE) { sb.Append(')'); continue; }
                if (t == (int)SpecialToken.BRACE_OPEN) { sb.Append('{'); continue; }
                if (t == (int)SpecialToken.BRACE_CLOSE) { sb.Append('}'); continue; }
                if (t == (int)SpecialToken.BRACKET_OPEN) { sb.Append('['); continue; }
                if (t == (int)SpecialToken.BRACKET_CLOSE) { sb.Append(']'); continue; }
                if (t == (int)SpecialToken.ANGLE_OPEN) { sb.Append('<'); continue; }
                if (t == (int)SpecialToken.ANGLE_CLOSE) { sb.Append('>'); continue; }

                if (VocabInverse.TryGetValue(t, out var s))
                {
                    sb.Append(s);
                }
            }
            return sb.ToString();
        }

        // ===== Persistencia =====
        public void Save(string path)
        {
            var settings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
                Formatting = Formatting.Indented
            };

            var json = JsonConvert.SerializeObject(Vocab, settings);
            System.IO.File.WriteAllText(path, json, Encoding.UTF8);
        }

        public void Load(string path)
        {
            var json = System.IO.File.ReadAllText(path, Encoding.UTF8);
            Vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
            VocabInverse = Vocab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }
    }
}