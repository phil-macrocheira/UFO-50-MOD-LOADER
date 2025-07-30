namespace UFO_50_MOD_INSTALLER
{
    public static class StringSimilarity
    {
        public static double CalculateTitleSimilarity(string titleA, string titleB) {
            if (string.IsNullOrEmpty(titleA) || string.IsNullOrEmpty(titleB)) return 0.0;

            var wordsA = new HashSet<string>(titleA.ToLower().Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries));
            var wordsB = new HashSet<string>(titleB.ToLower().Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries));

            if (wordsA.Count == 0 || wordsB.Count == 0) return 0.0;

            double commonWords = wordsA.Intersect(wordsB).Count();

            // Use the smaller word count as the denominator to better handle partial matches
            double totalWords = Math.Min(wordsA.Count, wordsB.Count);

            if (totalWords == 0) return 0.0;

            return commonWords / totalWords;
        }
    }
}