using System.Text.RegularExpressions;
using Backend.Classifier.Models;
using Backend.Models;
using Backend.DataCollector.Models;

namespace Backend.Classifier;

public sealed class RuleBasedClassifier : IClassifier
{
    private static readonly Regex[] ApplicationRules = BuildApplicationRules();
    private static readonly Regex[] WebsiteRules = BuildWebsiteRules();

    private readonly ClassifierRuleStore _ruleStore = new();
    private List<CompiledCategoryRule> _customApplicationRules = new();
    private List<CompiledCategoryRule> _customWebsiteRules = new();
    private DateTime? _rulesLastWriteUtc;

    public int? ClassifyAsync(ApplicationRecord record)
    {
        EnsureCustomRulesLoaded();
        var text = $"{record.ClassName} {record.ProcessName}".ToLowerInvariant();

        foreach (var rule in _customApplicationRules)
            if (rule.IsMatch(record))
                return rule.CategoryId;

        foreach (var rule in ApplicationRules)
            if (rule.IsMatch(text))
                return GetRuleCategory(rule);

        return null;
    }

    public IEnumerable<int?> ClassifyAsync(IEnumerable<ApplicationRecord> records)
    {
        foreach (var r in records)
            yield return ClassifyAsync(r);
    }

    public int? ClassifyAsync(BrowserRecord record)
    {
        EnsureCustomRulesLoaded();
        var text = BuildBrowserText(record);
        if (string.IsNullOrWhiteSpace(text)) return null;

        foreach (var rule in _customWebsiteRules)
            if (rule.IsMatch(record))
                return rule.CategoryId;

        foreach (var rule in WebsiteRules)
            if (rule.IsMatch(text))
                return GetRuleCategory(rule);

        return null;
    }

    public IEnumerable<int?> ClassifyAsync(IEnumerable<BrowserRecord> records)
    {
        foreach (var r in records)
            yield return ClassifyAsync(r);
    }

    private static string BuildBrowserText(BrowserRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Url)) return string.Empty;
        return Uri.TryCreate(record.Url, UriKind.Absolute, out var uri)
            ? $"{uri.Host} {uri.AbsolutePath} {record.Url}".ToLowerInvariant()
            : record.Url.ToLowerInvariant();
    }

    private static int GetRuleCategory(Regex rule)
    {
        var pattern = rule.ToString();
        return pattern.Contains("chrome") || pattern.Contains("firefox") ? 2 :
               pattern.Contains("slack") || pattern.Contains("discord") ? 4 :
               pattern.Contains("visual studio") || pattern.Contains("vscode") ? 5 :
               pattern.Contains("explorer") || pattern.Contains("nautilus") ? 8 :
               pattern.Contains("steam") || pattern.Contains("epicgames") ? 11 :
               pattern.Contains("photoshop") || pattern.Contains("gimp") ? 15 :
               pattern.Contains("blender") || pattern.Contains("maya") ? 16 :
               pattern.Contains("vlc") || pattern.Contains("mpv") ? 18 :
               pattern.Contains("7z") || pattern.Contains("winrar") ? 19 :
               pattern.Contains("winword") || pattern.Contains("writer") || pattern.Contains("excel") || pattern.Contains("calc") || pattern.Contains("powerpoint") || pattern.Contains("impress") ? 21 :
               pattern.Contains("notepad") || pattern.Contains("sublime") ? 32 :
               pattern.Contains("mstsc") || pattern.Contains("remmina") ? 38 :
               1;
    }

    private static Regex R(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

    private static Regex[] BuildApplicationRules() => new[]
    {
        R(@"(chrome|chromium|firefox|librewolf|brave|edge|opera|vivaldi|waterfox|palemoon|msedge|google-chrome|navigator|browser)"),
        R(@"(slack|discord|teams|skype|telegram|whatsapp|signal|viber|zoom|webex|element|matrix|mattermost|rocketchat|linphone|jitsi|ringcentral|messenger)"),
        R(@"(visual studio|vscode|code-oss|rider|intellij|idea|pycharm|clion|goland|webstorm|phpstorm|eclipse|netbeans|devenv|dotnet|msbuild|nuget|gcc|g\+\+|clang|cmake|make|ninja|gradle|maven|node|npm|yarn|cargo|rustc|go build|postman|insomnia|docker|kubectl|wireshark)"),
        R(@"(explorer\.exe|explorer|nautilus|dolphin|thunar|pcmanfm|nemo|caja|finder|file manager|filezilla)"),
        R(@"(steam|epicgameslauncher|gog|battle\.net|itch|riotclient|game|launcher)"),
        R(@"(photoshop|gimp|krita|affinity photo|paint\.net|coreldraw|inkscape|illustrator|photo editor|image editor)"),
        R(@"(blender|maya|3ds ?max|houdini|cinema ?4d|zbrush|substance|unreal editor|unity editor|godot)"),
        R(@"(vlc|mpv|mplayer|kdenlive|premiere|davinci resolve|after effects|obs|shotcut|capcut|video editor|media player)"),
        R(@"(7z|7-zip|winrar|rar|tar|gzip|bzip2|xz|peazip|ark)"),
        R(@"(winword|word|libreoffice writer|onlyoffice.*document|wps.*writer)"),
        R(@"(excel|libreoffice calc|onlyoffice.*spreadsheet|wps.*spreadsheet)"),
        R(@"(powerpoint|libreoffice impress|onlyoffice.*presentation|wps.*presentation)"),
        R(@"(notepad(\+\+)?|gedit|kate|sublime|vim|nvim|emacs|micro|xed)"),
        R(@"(mstsc|remmina|anydesk|teamviewer|rustdesk|realvnc|tightvnc|nomachine)"),
        R(@"(pcsx2|retroarch|dolphin-emu|yuzu|ryujinx|citra|ppsspp|mame)"),
        R(@"(stellarium|celestia|kstars|cartes du ciel)")
    };

    private static Regex[] BuildWebsiteRules() => new[]
    {
        R(@"(mail\.google|outlook\.(office|live)|mail\.yahoo|proton\.(me|mail)|fastmail|mail\.zoho|icloud\.com/mail)"),
        R(@"(slack\.com|discord\.com|teams\.microsoft\.com|meet\.google\.com|web\.whatsapp\.com|web\.telegram\.org|messenger\.com|zoom\.us|webex\.com)"),
        R(@"(github\.com|gitlab\.com|bitbucket\.org|stackoverflow\.com|stackexchange\.com|superuser\.com|serverfault\.com|npmjs\.com|nuget\.org|pypi\.org|rubygems\.org|crates\.io|(developer|learn|docs)\.microsoft\.com|developer\.mozilla\.org|mdn\.mozilla\.org|readthedocs\.io|jetbrains\.com|atlassian\.net|vercel\.com|netlify\.com)"),
        R(@"(docs\.google\.com|sheets\.google\.com|slides\.google\.com|office\.com|microsoft365\.com)"),
        R(@"(store\.steampowered\.com|steampowered\.com|epicgames\.com|gog\.com|itch\.io|battle\.net|roblox\.com)"),
        R(@"(figma\.com|canva\.com|photopea\.com|dribbble\.com|behance\.net)"),
        R(@"(webflow\.com|wix\.com|squarespace\.com)"),
        R(@"(youtube\.com|youtu\.be|netflix\.com|spotify\.com|twitch\.tv|vimeo\.com|soundcloud\.com|hulu\.com|disneyplus\.com|primevideo\.com)"),
        R(@"(notion\.so|trello\.com|asana\.com|clickup\.com|todoist\.com|monday\.com|linear\.app|miro\.com|evernote\.com)"),
        R(@"(reddit\.com|linkedin\.com|x\.com|twitter\.com|facebook\.com|instagram\.com|threads\.net)"),
        R(@"(wikipedia\.org|wiktionary\.org|britannica\.com|investopedia\.com|arxiv\.org|scholar\.google\.com)"),
        R(@"(paypal\.com|stripe\.com|wise\.com|revolut\.com|quickbooks\.intuit\.com|xero\.com|banking|onlinebanking)"),
        R(@"(drive\.google\.com|dropbox\.com|onedrive\.live\.com|onedrive\.com|box\.com|wetransfer\.com)")
    };

    private void EnsureCustomRulesLoaded()
    {
        DateTime? currentTimestamp = null;
        if (File.Exists(ClassifierRuleStore.RulesFilePath))
            currentTimestamp = File.GetLastWriteTimeUtc(ClassifierRuleStore.RulesFilePath);

        if (_rulesLastWriteUtc == currentTimestamp) return;

        var rules = _ruleStore.LoadRules().Where(r => r.Enabled).ToList();
        _customApplicationRules = rules.Where(r => r.Target == CategoryRuleTarget.Application).Select(CompiledCategoryRule.Create).ToList();
        _customWebsiteRules = rules.Where(r => r.Target == CategoryRuleTarget.Website).Select(CompiledCategoryRule.Create).ToList();
        _rulesLastWriteUtc = currentTimestamp;
    }

    private sealed class CompiledCategoryRule
    {
        private readonly CategoryRule _rule;
        private readonly Regex? _regex;

        private CompiledCategoryRule(CategoryRule rule, Regex? regex) { _rule = rule; _regex = regex; }
        public int CategoryId => _rule.CategoryId;

        public static CompiledCategoryRule Create(CategoryRule rule)
        {
            var regex = rule.MatchType == CategoryRuleMatchType.Regex
                ? new Regex(rule.Pattern, RegexOptions.Compiled | (rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None), TimeSpan.FromMilliseconds(50))
                : null;
            return new CompiledCategoryRule(rule, regex);
        }

        public bool IsMatch(ApplicationRecord record)
            => CategoryRuleMatcher.GetApplicationCandidates(_rule.Field, record).Any(Matches);

        public bool IsMatch(BrowserRecord record)
            => CategoryRuleMatcher.GetBrowserCandidates(_rule.Field, record).Any(Matches);

        private bool Matches(string candidate)
            => _regex is not null ? _regex.IsMatch(candidate) : CategoryRuleMatcher.Matches(_rule, candidate);
    }
}