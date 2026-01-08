namespace MoNeriSharp.Map
{
    public static class EmotionMap
    {
        // Mapa de etiquetas → índice
        public static readonly Dictionary<string, int> LabelToIndex = new Dictionary<string, int>
        {
            {"alegria", 0},
            {"tristeza", 1},
            {"enojo", 2},
            {"miedo", 3},
            {"sorpresa", 4},
            {"neutral", 5}
        };

        // Diccionario inverso para mostrar nombres legibles
        public static readonly Dictionary<int, string> IndexToName = new Dictionary<int, string>
        {
            {0, "Alegría"},
            {1, "Tristeza"},
            {2, "Enojo"},
            {3, "Miedo"},
            {4, "Sorpresa"},
            {5, "Neutral"}
        };
    }
}