namespace FootballBoss.Services;

/// <summary>
/// Generates random player and staff names using the same algorithm as
/// FOOT.BAS subroutine 98 (lines 173–183).
///
/// Format: Initial.ConsonantVowelConsonantSuffix  e.g. "J.TaBson "
///   Initial   = capital letter A–V  (CHR$(65+RND*22))
///   Consonants from H$ = "BCDFGHJKLMNPRSTVW"  (17 chars)
///   Vowels    from M$ = "AEIOU"                (5 chars)
///   Suffix    from E$(1–26) loaded from data.fd (hardcoded below)
///
/// The name is retried if the result would exceed 8 characters, then
/// right-padded to exactly 8 characters — matching V$(I) storage.
/// </summary>
public static class NameGenerationService
{
    private const string Consonants = "BCDFGHJKLMNPRSTVW";   // H$ in FOOT.BAS
    private const string Vowels     = "AEIOU";                // M$

    /// <summary>
    /// 26 surname suffixes corresponding to E$(1)–E$(26) from data.fd.
    /// Chosen to produce English-sounding footballer names when appended to
    /// a three-letter root such as "Bal", "Gre", or "Hor".
    /// </summary>
    private static readonly string[] SurnameSuffixes =
    {
        "son",    // 1
        "man",    // 2
        "ton",    // 3
        "ley",    // 4
        "ford",   // 5
        "wick",   // 6
        "ner",    // 7
        "er",     // 8
        "ham",    // 9
        "wood",   // 10
        "bury",   // 11
        "worth",  // 12
        "field",  // 13
        "ard",    // 14
        "bridge", // 15
        "ling",   // 16
        "shaw",   // 17
        "burn",   // 18
        "ster",   // 19
        "land",   // 20
        "well",   // 21
        "ney",    // 22
        "ock",    // 23
        "gate",   // 24
        "sworth", // 25
        "ment"    // 26
    };

    /// <summary>
    /// Generates a single padded 8-character name, retrying until the result
    /// is ≤ 8 characters (BASIC line 181: IF LEN(TEMP$)>8 THEN 98).
    /// </summary>
    public static string GenerateName(Random rng)
    {
        string name;
        do
        {
            char   initial    = (char)(65 + rng.Next(22));          // A–V  (z9)
            int    suffixIdx  = rng.Next(SurnameSuffixes.Length);    // RA (0-based)
            char   vowel      = char.ToLower(Vowels[rng.Next(5)]);   // RB
            char   consonant1 = Consonants[rng.Next(17)];            // rc
            char   consonant2 = Consonants[rng.Next(17)];            // rd

            name = $"{initial}.{consonant1}{vowel}{consonant2}{SurnameSuffixes[suffixIdx]}";
        }
        while (name.Length > 8);

        return name.PadRight(8);   // TEMP$=TEMP$+SPACE$(8-LEN(TEMP$))
    }

    /// <summary>
    /// Generates a batch of unique names — useful when populating a full squad.
    /// Retries on collisions so every name in the batch is distinct.
    /// </summary>
    public static List<string> GenerateUniqueNames(int count, Random rng)
    {
        var names = new HashSet<string>(count);
        while (names.Count < count)
            names.Add(GenerateName(rng));
        return names.ToList();
    }
}
