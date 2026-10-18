namespace VoixDuQuartier.Helpers;

/// <summary>Construit le titre d'un signalement à partir de sa catégorie et de sa description.</summary>
public static class TitleBuilder
{
    public const int MaxExcerptLength = 45;

    public static string Build(string category, string description)
    {
        var text = (description ?? string.Empty).Trim();

        var end = text.IndexOfAny(new[] { '.', '!', '?', '\n' });
        if (end > 0) text = text[..end].Trim();

        if (text.Length > MaxExcerptLength)
            text = text[..MaxExcerptLength].TrimEnd() + "…";

        return text.Length == 0 ? category : $"{category} : {text}";
    }
}
