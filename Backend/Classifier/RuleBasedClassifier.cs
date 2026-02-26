using Backend.Classifier.Models;
using Backend.Models;

namespace Backend.Classifier;

public class RuleBasedClassifier : IClassifier
{
    public Category ClassifyAsync(SessionRecord record)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Category> ClassifyAsync(IEnumerable<SessionRecord> records)
    {
        throw new NotImplementedException();
    }
}



/*

public enum Category
{
    Development,
    Productivity,
    Browsing,
    Media,
    Games,
    Communication,
    System,
    Other
}


public static class ActivityClassifier
{
    // ---------- Regex / keyword sets ----------

    static readonly Regex MediaExt =
        new(@"\.(mkv|mp4|avi|webm|mp3|flac|wav)\b", RegexOptions.IgnoreCase);

    static readonly Regex DevExt =
        new(@"\.(cs|cpp|h|hpp|axaml|xaml|js|ts|py|java|rs|go)\b", RegexOptions.IgnoreCase);

    static readonly string[] BrowserProcesses =
    {
        "librewolf", "firefox", "chrome", "chromium", "brave", "edge"
    };

    static readonly HashSet<string> EntertainmentDomains =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "youtube", "twitch", "netflix", "crunchyroll"
        };

    // ---------- Public API ----------

    public static (Category category, double confidence) Classify(ApplicationDto row)
    {
        var features = ExtractFeatures(row);
        return Decide(features);
    }

    // ---------- Feature extraction ----------

    static Features ExtractFeatures(ApplicationDto row)
    {
        var proc = row.ClassName.ToLowerInvariant();
        var title = row.WindowTitle ?? "";

        return new Features
        {
            IsBrowser = BrowserProcesses.Any(p => proc.Contains(p)),
            HasMediaExt = MediaExt.IsMatch(title),
            HasDevExt = DevExt.IsMatch(title),
            Domain = ExtractDomain(title),
        };
    }

    static string? ExtractDomain(string title)
    {
        // works for "YouTube — LibreWolf" and URLs
        var m = Regex.Match(title, @"([a-z0-9\-]+)\.(com|org|net|io|edu)",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    // ---------- Decision logic ----------

    static (Category, double) Decide(Features f)
    {
        double scoreDev = 0, scoreMedia = 0, scoreGame = 0, scoreBrowse = 0;

        if (f.HasDevExt) scoreDev += 3;
        if (f.HasMediaExt) scoreMedia += 4;

        if (f.IsBrowser)
        {
            scoreBrowse += 1;

            if (f.Domain != null)
            {
                if (EntertainmentDomains.Contains(f.Domain))
                    scoreMedia += 3;
                else
                    scoreBrowse += 1;
            }
        }

        if (f.IsFullscreenCandidate && f.ActiveSeconds > 1800)
            scoreGame += 2;

        var scores = new Dictionary<Category, double>
        {
            [Category.Development] = scoreDev,
            [Category.Media] = scoreMedia,
            [Category.Games] = scoreGame,
            [Category.Browsing] = scoreBrowse,
            [Category.Productivity] = scoreDev * 0.5 + scoreBrowse * 0.5
        };

        var best = scores.OrderByDescending(x => x.Value).First();

        double confidence = Math.Min(1.0, best.Value / 5.0);

        return best.Value == 0
            ? (Category.Other, 0.2)
            : (best.Key, confidence);
    }

    // ---------- Internal feature struct ----------

    record Features
    {
        public bool IsBrowser;
        public bool HasMediaExt;
        public bool HasDevExt;
        public string? Domain;
        public double ActiveSeconds;
        public bool IsFullscreenCandidate;
    }
}
*/