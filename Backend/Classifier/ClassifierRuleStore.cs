using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Classifier.Models;
using Backend.Models;

namespace Backend.Classifier;

public sealed class ClassifierRuleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string RulesFilePath => Path.Combine(Settings.DataDirectory, "classifier-rules.json");

    public IReadOnlyList<CategoryRule> LoadRules()
    {
        try
        {
            if (!File.Exists(RulesFilePath))
            {
                return [];
            }

            var json = File.ReadAllText(RulesFilePath);
            var document = JsonSerializer.Deserialize<ClassifierRuleDocument>(json, JsonOptions);
            if (document?.Rules == null)
            {
                return [];
            }

            return document.Rules
                .Where(rule => CategoryRuleMatcher.TryValidate(rule, out _))
                .OrderBy(rule => rule.Priority)
                .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => rule.Pattern, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public void SaveRules(IEnumerable<CategoryRule> rules)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RulesFilePath)!);

        var document = new ClassifierRuleDocument
        {
            Rules = rules
                .Select(Clone)
                .OrderBy(rule => rule.Priority)
                .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => rule.Pattern, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(RulesFilePath, json);
    }

    private static CategoryRule Clone(CategoryRule rule)
    {
        return new CategoryRule
        {
            Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id,
            CategoryId = rule.CategoryId,
            Name = rule.Name ?? string.Empty,
            Target = rule.Target,
            Field = rule.Field,
            MatchType = rule.MatchType,
            Pattern = rule.Pattern ?? string.Empty,
            Priority = rule.Priority,
            Enabled = rule.Enabled,
            IgnoreCase = rule.IgnoreCase,
            Notes = string.IsNullOrWhiteSpace(rule.Notes) ? null : rule.Notes.Trim()
        };
    }

    private sealed class ClassifierRuleDocument
    {
        public int Version { get; set; } = 1;
        public List<CategoryRule> Rules { get; set; } = [];
    }
}
