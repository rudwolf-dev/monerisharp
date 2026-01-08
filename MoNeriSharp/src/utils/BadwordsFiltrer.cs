namespace MoNeriSharp.Utils
{
    public static class BadwordFilter
    {
        // Lista de palabras erróneas, raras o en otro idioma
        private static readonly HashSet<string> InvalidTokens = new HashSet<string>
        {
    // Groserías en español (ejemplo)
    "puta", "puto", "mierda", "pendejo", "chingar", "chingada",
    "cabron", "pinche", "culero", "verga", "coño",
    "imbecil", "idiota", "estupido", "joto", "maricon",
    "perra", "chingon", "chingadera", "pito", "naco", "zorra",

    // Groserías en inglés (ejemplo)
    "fuck", "shit", "bitch", "asshole", "bastard", "dick", "cunt",
    "motherfucker", "son of a bitch", "jerk", "moron", "retard",
    "slut", "whore", "hoe", "prick", "wanker"
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