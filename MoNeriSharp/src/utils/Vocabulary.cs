using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoNeriSharp.Utils
{
    public static class Vocabulary
    {
        // ===== Construcción de vocabulario por palabras =====
        public static void BuildWordVocabulary(
            Dictionary<string, int> vocab,
            Dictionary<int, string> vocabInverse,
            IEnumerable<string> corpus,
            bool caseSensitive,
            int vocabSize)
        {
            var freq = new Dictionary<string, int>();

            foreach (var line in corpus)
            {
                foreach (var token in Format.TokenizePreservingFormat(line))
                {
                    var word = caseSensitive ? token : token.ToLowerInvariant();
                    if (!freq.ContainsKey(word)) freq[word] = 0;
                    freq[word]++;
                }
            }

            var mostCommon = freq.OrderByDescending(kvp => kvp.Value)
                                 .Take(vocabSize - vocab.Count)
                                 .Select(kvp => kvp.Key);

            int idx = vocab.Count;
            foreach (var word in mostCommon)
            {
                if (!vocab.ContainsKey(word))
                {
                    vocab[word] = idx;
                    vocabInverse[idx] = word;
                    idx++;
                }
            }
        }

        public static void ExpandWordVocabulary(
            Dictionary<string, int> vocab,
            Dictionary<int, string> vocabInverse,
            IEnumerable<string> corpus,
            bool caseSensitive,
            int maxNewWords)
        {
            var freq = new Dictionary<string, int>();

            foreach (var line in corpus)
            {
                foreach (var token in Format.TokenizePreservingFormat(line))
                {
                    var word = caseSensitive ? token : token.ToLowerInvariant();
                    if (!vocab.ContainsKey(word))
                    {
                        if (!freq.ContainsKey(word)) freq[word] = 0;
                        freq[word]++;
                    }
                }
            }

            var newWords = freq.OrderByDescending(kvp => kvp.Value)
                               .Take(maxNewWords)
                               .Select(kvp => kvp.Key);

            int idx = vocab.Count;
            foreach (var word in newWords)
            {
                vocab[word] = idx;
                vocabInverse[idx] = word;
                idx++;
            }
        }

        // ===== Construcción y expansión de subword (BPE) =====
        public static void BuildSubwordVocabulary(
            out Dictionary<string, int> subwordVocab,
            out List<(string, string)> merges,
            IEnumerable<string> corpus,
            int vocabSize)
        {
            var freq = new Dictionary<string, int>();

            foreach (var line in corpus)
            {
                var symbols = Format.TokenizePreservingFormat(line).ToList();
                if (symbols.Count == 0) continue;

                var chars = string.Join(" ", symbols);
                if (!freq.ContainsKey(chars)) freq[chars] = 0;
                freq[chars]++;
            }

            merges = new List<(string, string)>();

            while (freq.Count < vocabSize)
            {
                var pairs = new Dictionary<(string, string), int>();
                foreach (var entry in freq.Keys)
                {
                    var symbols = entry.Split(' ');
                    for (int i = 0; i < symbols.Length - 1; i++)
                    {
                        var pair = (symbols[i], symbols[i + 1]);
                        if (!pairs.ContainsKey(pair)) pairs[pair] = 0;
                        pairs[pair]++;
                    }
                }

                if (pairs.Count == 0) break;
                var best = pairs.OrderByDescending(p => p.Value).First().Key;
                merges.Add(best);

                var newFreq = new Dictionary<string, int>();
                foreach (var entry in freq.Keys)
                {
                    var symbols = entry.Split(' ').ToList();
                    for (int i = 0; i < symbols.Count - 1; i++)
                    {
                        if (symbols[i] == best.Item1 && symbols[i + 1] == best.Item2)
                        {
                            symbols[i] = symbols[i] + symbols[i + 1];
                            symbols.RemoveAt(i + 1);
                            i = Math.Max(i - 1, 0);
                        }
                    }
                    var newWord = string.Join(" ", symbols);
                    if (!newFreq.ContainsKey(newWord)) newFreq[newWord] = 0;
                    newFreq[newWord] += freq[entry];
                }
                freq = newFreq;
            }

            subwordVocab = new Dictionary<string, int>();
            int idx = 0;
            foreach (var entry in freq.Keys)
            {
                foreach (var symbol in entry.Split(' '))
                {
                    if (!subwordVocab.ContainsKey(symbol))
                    {
                        subwordVocab[symbol] = idx++;
                    }
                }
            }
        }

        // ===== Construcción y expansión de sílabas =====
        public static void BuildSyllableVocabulary(
            Dictionary<string, int> vocab,
            Dictionary<int, string> vocabInverse,
            IEnumerable<string> corpus,
            bool caseSensitive,
            int vocabSize)
        {
            var freq = new Dictionary<string, int>();

            foreach (var line in corpus)
            {
                foreach (var token in Format.TokenizePreservingFormat(line))
                {
                    if (token == Format.SPACE || token == Format.NEWLINE)
                    {
                        if (!freq.ContainsKey(token)) freq[token] = 0;
                        freq[token]++;
                    }
                    else
                    {
                        var syllables = Silabificador.SplitIntoSyllables(token);
                        foreach (var s in syllables)
                        {
                            var t = caseSensitive ? s : s.ToLowerInvariant();
                            if (!freq.ContainsKey(t)) freq[t] = 0;
                            freq[t]++;
                        }
                    }
                }
            }

            var mostCommon = freq.OrderByDescending(kvp => kvp.Value)
                                 .Take(vocabSize - vocab.Count)
                                 .Select(kvp => kvp.Key);

            int idx = vocab.Count;
            foreach (var syllable in mostCommon)
            {
                if (!vocab.ContainsKey(syllable))
                {
                    vocab[syllable] = idx;
                    vocabInverse[idx] = syllable;
                    idx++;
                }
            }
        }

        public static void ExpandSyllableVocabulary(
            Dictionary<string, int> vocab,
            Dictionary<int, string> vocabInverse,
            IEnumerable<string> corpus,
            bool caseSensitive,
            int maxNewSyllables)
        {
            var freq = new Dictionary<string, int>();

            foreach (var line in corpus)
            {
                foreach (var token in Format.TokenizePreservingFormat(line))
                {
                    if (token == Format.SPACE || token == Format.NEWLINE)
                    {
                        if (!vocab.ContainsKey(token))
                        {
                            if (!freq.ContainsKey(token)) freq[token] = 0;
                            freq[token]++;
                        }
                    }
                    else
                    {
                        var syllables = Silabificador.SplitIntoSyllables(token);
                        foreach (var s in syllables)
                        {
                            var t = caseSensitive ? s : s.ToLowerInvariant();
                            if (!vocab.ContainsKey(t))
                            {
                                if (!freq.ContainsKey(t)) freq[t] = 0;
                                freq[t]++;
                            }
                        }
                    }
                }
            }

            var newSyllables = freq.OrderByDescending(kvp => kvp.Value)
                                   .Take(maxNewSyllables)
                                   .Select(kvp => kvp.Key);

            int idx = vocab.Count;
            foreach (var syllable in newSyllables)
            {
                vocab[syllable] = idx;
                vocabInverse[idx] = syllable;
                idx++;
            }
        }
    }
}