namespace MPDCtrlX.Models;

public enum SearchTags
{
    Title, Artist, Album, Genre
}

public class SearchOption(SearchTags key, string label)
{
    public SearchTags Key { get; set; } = key;
    public string Label { get; set; } = label;
}
