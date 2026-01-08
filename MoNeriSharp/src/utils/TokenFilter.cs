namespace MoNeriSharp.Utils
{
    public static class TokenFilter
    {
        // Lista de palabras erróneas, raras o en otro idioma
        private static readonly HashSet<string> InvalidTokens = new HashSet<string>
        {
    // Errores comunes / ruido
    "asdfgh", "qwerty", "errorToken", "palabra_rara",
    "undefined", "null", "nan", "12345", "!!!", "???",
    "test", "demo", "lorem", "ipsum", "xxx", "zzz", "aaaa",
    "11111", "00000", "wtf", "lol", "rofl",

    // Otros idiomas (ejemplo)
    "bonjour", "ciao", "hallo", "こんにちは", "привет",
    "salut", "merci", "au revoir", "grazie", "arrivederci",
    "tschüss", "danke", "さようなら", "ありがとう",
    "до свидания", "спасибо", "olá", "tchau", "obrigado",
        };

        /// <summary>
        /// Limpia el corpus eliminando tokens inválidos.
        /// </summary>
        public static List<string> CleanCorpus(List<string> corpus)
        {
            var cleaned = new List<string>();

            foreach (var text in corpus)
            {
                var filteredTokens = text
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => !InvalidTokens.Contains(token.ToLowerInvariant()));

                if (filteredTokens.Any())
                {
                    cleaned.Add(string.Join(" ", filteredTokens));
                }
            }

            return cleaned;
        }

        /// <summary>
        /// Permite añadir nuevas palabras inválidas dinámicamente.
        /// </summary>
        public static void AddInvalidToken(string token)
        {
            InvalidTokens.Add(token.ToLowerInvariant());
        }

        /// <summary>
        /// Permite quitar palabras inválidas de la lista.
        /// </summary>
        public static void RemoveInvalidToken(string token)
        {
            InvalidTokens.Remove(token.ToLowerInvariant());
        }
    }
}