namespace MPDCtrl.Models;

public enum SearchTags
{
    Title, Artist, Album, Genre, Any
}

public sealed class SearchOption(SearchTags key, string label)
{
    public SearchTags Key { get; set; } = key;
    public string Label { get; set; } = label;
}

public enum SearchShiki
{
    Contains, Equals
}

public sealed class SearchWith(SearchShiki shiki, string label)
{
    public SearchShiki Shiki { get; set; } = shiki;
    public string Label { get; set; } = label;
}