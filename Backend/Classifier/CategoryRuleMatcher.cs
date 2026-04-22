using System.Text.RegularExpressions;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.Classifier;

public static class CategoryRuleMatcher
{
    public static bool TryValidate(CategoryRule rule, out string error)
    {
        if (rule.CategoryId <= 0)
        {
            error = "Categoria regulii este obligatorie.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            error = "Modelul de potrivire este obligatoriu.";
            return false;
        }

        if (!SupportsField(rule.Target, rule.Field))
        {
            error = "Campul ales nu este compatibil cu tinta regulii.";
            return false;
        }

        if (rule.MatchType == CategoryRuleMatchType.Regex)
        {
            try
            {
                _ = new Regex(rule.Pattern, BuildRegexOptions(rule.IgnoreCase));
            }
            catch (ArgumentException ex)
            {
                error = $"Regex invalid: {ex.Message}";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static bool SupportsField(CategoryRuleTarget target, CategoryRuleField field)
    {
        return target switch
        {
            CategoryRuleTarget.Application => field is CategoryRuleField.Any or CategoryRuleField.ProcessName or CategoryRuleField.WindowTitle or CategoryRuleField.ClassName,
            CategoryRuleTarget.Website => field is CategoryRuleField.Any or CategoryRuleField.Url or CategoryRuleField.Host or CategoryRuleField.Path,
            _ => false
        };
    }

    public static bool IsMatch(CategoryRule rule, ApplicationRecord record)
    {
        if (!rule.Enabled || rule.Target != CategoryRuleTarget.Application || !TryValidate(rule, out _))
        {
            return false;
        }

        return GetApplicationCandidates(rule.Field, record).Any(candidate => Matches(rule, candidate));
    }

    public static bool IsMatch(CategoryRule rule, BrowserRecord record)
    {
        if (!rule.Enabled || rule.Target != CategoryRuleTarget.Website || !TryValidate(rule, out _))
        {
            return false;
        }

        return GetBrowserCandidates(rule.Field, record).Any(candidate => Matches(rule, candidate));
    }

    internal static IEnumerable<string> GetApplicationCandidates(CategoryRuleField field, ApplicationRecord record)
    {
        return field switch
        {
            CategoryRuleField.Any => Values(record.ProcessName, record.WindowName, record.ClassName),
            CategoryRuleField.ProcessName => Values(record.ProcessName),
            CategoryRuleField.WindowTitle => Values(record.WindowName),
            CategoryRuleField.ClassName => Values(record.ClassName),
            _ => []
        };
    }

    internal static IEnumerable<string> GetBrowserCandidates(CategoryRuleField field, BrowserRecord record)
    {
        var path = string.Empty;
        if (Uri.TryCreate(record.Url, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }

        return field switch
        {
            CategoryRuleField.Any => Values(record.Url, record.Domain, path),
            CategoryRuleField.Url => Values(record.Url),
            CategoryRuleField.Host => Values(record.Domain),
            CategoryRuleField.Path => Values(path),
            _ => []
        };
    }

    internal static bool Matches(CategoryRule rule, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var comparison = rule.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return rule.MatchType switch
        {
            CategoryRuleMatchType.Contains => candidate.Contains(rule.Pattern, comparison),
            CategoryRuleMatchType.Exact => string.Equals(candidate, rule.Pattern, comparison),
            CategoryRuleMatchType.StartsWith => candidate.StartsWith(rule.Pattern, comparison),
            CategoryRuleMatchType.EndsWith => candidate.EndsWith(rule.Pattern, comparison),
            CategoryRuleMatchType.Regex => Regex.IsMatch(candidate, rule.Pattern, BuildRegexOptions(rule.IgnoreCase)),
            _ => false
        };
    }

    private static RegexOptions BuildRegexOptions(bool ignoreCase)
    {
        return RegexOptions.Compiled | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
    }

    private static IEnumerable<string> Values(params string?[] values)
    {
        return values.Where(value => !string.IsNullOrWhiteSpace(value))!
            .Select(value => value!);
    }
}
