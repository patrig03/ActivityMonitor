namespace Backend.Classifier.Models;

public enum CategoryRuleTarget
{
    Application,
    Website
}

public enum CategoryRuleField
{
    Any,
    ProcessName,
    WindowTitle,
    ClassName,
    Url,
    Host,
    Path
}

public enum CategoryRuleMatchType
{
    Contains,
    Exact,
    StartsWith,
    EndsWith,
    Regex
}

public sealed class CategoryRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryRuleTarget Target { get; set; } = CategoryRuleTarget.Application;
    public CategoryRuleField Field { get; set; } = CategoryRuleField.ProcessName;
    public CategoryRuleMatchType MatchType { get; set; } = CategoryRuleMatchType.Contains;
    public string Pattern { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public bool Enabled { get; set; } = true;
    public bool IgnoreCase { get; set; } = true;
    public string? Notes { get; set; }
}
