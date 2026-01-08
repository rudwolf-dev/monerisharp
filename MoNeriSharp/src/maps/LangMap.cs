namespace MoNeriSharp.Map
{
    public static class LangMap
    {
        // Mapa de códigos ISO → índice
        public static readonly Dictionary<string, int> CodeToIndex = new Dictionary<string, int>
        {
            {"es", 0}, {"en", 1}, {"pt", 2}, {"fr", 3}, {"de", 4},
            {"it", 5}, {"ru", 6}, {"zh", 7}, {"ar", 8}, {"hi", 9},
            {"pl", 10}, {"bg", 11}, {"sw", 12}, {"tr", 13}, {"th", 14},
            {"el", 15}, {"ur", 16}
            // añade más si tu CSV trae otros códigos
        };

        // Diccionario inverso para mostrar nombres legibles
        public static readonly Dictionary<int, string> IndexToName = new Dictionary<int, string>
        {
            {0, "Español"}, {1, "Inglés"}, {2, "Portugués"}, {3, "Francés"}, {4, "Alemán"},
            {5, "Italiano"}, {6, "Ruso"}, {7, "Chino"}, {8, "Árabe"}, {9, "Hindi"},
            {10, "Polaco"}, {11, "Búlgaro"}, {12, "Suajili"}, {13, "Turco"}, {14, "Tailandés"},
            {15, "Griego"}, {16, "Urdu"}
        };
    }
}