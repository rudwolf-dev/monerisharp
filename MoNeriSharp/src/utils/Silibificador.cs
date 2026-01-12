using System;
using System.Collections.Generic;
using System.Text;

public class Silabificador
{
    private static readonly string vowels = "aeiouáéíóúü";
    private static readonly string strongVowels = "aáeéoó";
    private static readonly string weakVowels = "iíuúü";

    private static readonly HashSet<string> inseparableGroups = new HashSet<string>
    {
        "br","bl","cr","cl","dr","fr","fl","gr","gl","pr","pl","tr"
    };

    private static bool IsVowel(char c) => vowels.Contains(c);
    private static bool IsStrong(char c) => strongVowels.Contains(c);
    private static bool IsWeak(char c) => weakVowels.Contains(c);
    private static bool HasAccent(char c) => "áéíóú".Contains(c);

    public static List<string> SplitIntoSyllables(string word)
    {
        word = word.ToLower();
        var syllables = new List<string>();
        var current = new StringBuilder();

        int i = 0;
        while (i < word.Length)
        {
            current.Append(word[i]);

            if (IsVowel(word[i]))
            {
                int next = i + 1;

                // Detectar hiato
                if (next < word.Length && IsVowel(word[next]))
                {
                    char v1 = word[i];
                    char v2 = word[next];

                    bool hiato = (IsStrong(v1) && IsStrong(v2)) ||
                                 (IsStrong(v1) && IsWeak(v2) && HasAccent(v2)) ||
                                 (IsWeak(v1) && IsStrong(v2) && HasAccent(v1));

                    if (!hiato)
                    {
                        // Diptongo/triptongo → no cortar
                        current.Append(word[next]);
                        i++;
                    }
                }

                // Manejo de consonantes siguientes
                int consonantCount = 0;
                int j = i + 1;
                while (j < word.Length && !IsVowel(word[j]))
                {
                    consonantCount++;
                    j++;
                }

                if (consonantCount == 1)
                {
                    syllables.Add(current.ToString());
                    current.Clear();
                }
                else if (consonantCount == 2)
                {
                    string group = word.Substring(i + 1, 2);
                    if (inseparableGroups.Contains(group))
                    {
                        syllables.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(word[i + 1]);
                        syllables.Add(current.ToString());
                        current.Clear();
                        i++;
                    }
                }
                else if (consonantCount > 2)
                {
                    current.Append(word[i + 1]);
                    syllables.Add(current.ToString());
                    current.Clear();
                    i++;
                }
            }

            i++;
        }

        if (current.Length > 0)
            syllables.Add(current.ToString());

        return syllables;
    }

    public static string JoinSyllables(List<string> syllables)
    {
        if (syllables == null || syllables.Count == 0)
            return string.Empty;

        return string.Concat(syllables);
    }

}