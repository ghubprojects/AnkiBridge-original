namespace AnkiBridge.Web.Common.Helpers;

public static class StringUtils
{
    /// <summary>
    /// Masks a word by replacing vowels with underscores 
    /// and randomly masking a small percentage of other characters.
    /// Eg: "anticipate" -> "_nt_c_p_t_"
    ///     "developer"  -> "d_v_l_p_r"
    /// </summary>
    /// <param name="word">The word to mask</param>
    /// <param name="randomMaskPercent">Percentage of non-vowel letters (0..1) to additionally mask</param>
    /// <returns>Masked word</returns>
    public static string Mask(string word, double randomMaskPercent = 0.2)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        if (word.Length <= 2)
            return new string('_', word.Length); // che toàn bộ nếu quá ngắn

        var rng = new Random();
        var vowels = "aeiouAEIOU";
        var chars = word.ToCharArray();

        // 1. Mask all vowels with '_'
        for (int i = 0; i < chars.Length; i++)
        {
            if (vowels.Contains(chars[i]))
                chars[i] = '_';
        }

        // 2. Randomly mask additional characters (non-vowels, non-edge)
        var candidateIndexes = Enumerable.Range(1, word.Length - 2) // skip first/last
                                         .Where(i => chars[i] != '_') // skip already-masked vowels
                                         .OrderBy(_ => rng.Next())
                                         .Take((int)(word.Length * randomMaskPercent));

        foreach (var i in candidateIndexes)
            chars[i] = '_';

        return new string(chars);
    }
}
