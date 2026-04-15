using System.Text.RegularExpressions;
using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.Classifier;

public class RuleBasedClassifier : IClassifier
{
    private readonly List<(int Category, Regex Pattern)> _applicationRules = new()
    {
        (2, R("(chrome|chromium|firefox|librewolf|brave|edge|opera|vivaldi|waterfox|palemoon)")),
        (2, R("(msedge|google-chrome|navigator|browser)")),

        // 4 Chat / IM / Telephony
        (4, R("(slack|discord|teams|skype|telegram|whatsapp|signal|viber|zoom|webex|element|matrix|mattermost|rocketchat)")),
        (4, R("(linphone|jitsi|ringcentral|messenger)")),

        // 5 Programming / Software Engineering
        (5, R("(visual studio|vscode|code-oss|rider|intellij|idea|pycharm|clion|goland|webstorm|phpstorm|eclipse|netbeans)")),
        (5, R("(devenv|dotnet|msbuild|nuget|gcc|g\\+\\+|clang|cmake|make|ninja|gradle|maven|node|npm|yarn|cargo|rustc|go build)")),
        (5, R("(postman|insomnia|docker|kubectl|wireshark)")),

        // 8 File System
        (8, R("(explorer\\.exe|explorer|nautilus|dolphin|thunar|pcmanfm|nemo|caja|finder)")),
        (8, R("(file manager|filezilla)")),

        // 11 Games (broad)
        (11, R("(steam|epicgameslauncher|gog|battle\\.net|itch|riotclient)")),
        (11, R("(game|launcher)")),

        // 15 Graphics Editing
        (15, R("(photoshop|gimp|krita|affinity photo|paint\\.net|coreldraw|inkscape|illustrator)")),
        (15, R("(photo editor|image editor)")),

        // 16 Animation / Rendering / 3D
        (16, R("(blender|maya|3ds ?max|houdini|cinema ?4d|zbrush|substance)")),
        (16, R("(unreal editor|unity editor|godot)")),

        // 18 Video
        (18, R("(vlc|mpv|mplayer|kdenlive|premiere|davinci resolve|after effects|obs|shotcut|capcut)")),
        (18, R("(video editor|media player)")),

        // 19 Compression
        (19, R("(7z|7-zip|winrar|rar|tar|gzip|bzip2|xz|peazip|ark)")),

        // 20 Word Processing
        (20, R("(winword|word|libreoffice writer|onlyoffice.*document|wps.*writer)")),

        // 21 Spreadsheet
        (21, R("(excel|libreoffice calc|onlyoffice.*spreadsheet|wps.*spreadsheet)")),

        // 23 Presentation
        (23, R("(powerpoint|libreoffice impress|onlyoffice.*presentation|wps.*presentation)")),

        // 32 Text Editors
        (32, R("(notepad(\\+\\+)?|gedit|kate|sublime|vim|nvim|emacs|micro|xed)")),

        // 38 Remote Access
        (38, R("(mstsc|remmina|anydesk|teamviewer|rustdesk|realvnc|tightvnc|nomachine)")),

        // 51 Emulators
        (51, R("(pcsx2|retroarch|dolphin-emu|yuzu|ryujinx|citra|ppsspp|mame)")),

        // 53 Astronomy
        (53, R("(stellarium|celestia|kstars|cartes du ciel)"))
    };

    private readonly List<(int Category, Regex Pattern)> _websiteRules = new()
    {
        // 3 Email / news / groupware
        (3, R("(mail\\.google|outlook\\.(office|live)|mail\\.yahoo|proton\\.(me|mail)|fastmail|mail\\.zoho|icloud\\.com/mail)")),

        // 4 Chat / IM / Telephony
        (4, R("(slack\\.com|discord\\.com|teams\\.microsoft\\.com|meet\\.google\\.com|web\\.whatsapp\\.com|web\\.telegram\\.org|messenger\\.com|zoom\\.us|webex\\.com)")),

        // 5 Programming / Software Engineering
        (5, R("(github\\.com|gitlab\\.com|bitbucket\\.org|stackoverflow\\.com|stackexchange\\.com|superuser\\.com|serverfault\\.com|npmjs\\.com|nuget\\.org|pypi\\.org|rubygems\\.org|crates\\.io|pkg\\.go\\.dev)")),
        (5, R("((developer|learn|docs)\\.microsoft\\.com|developer\\.mozilla\\.org|mdn\\.mozilla\\.org|readthedocs\\.io|jetbrains\\.com|atlassian\\.net|vercel\\.com|netlify\\.com)")),

        // 9 Office suites
        (9, R("(docs\\.google\\.com|sheets\\.google\\.com|slides\\.google\\.com|office\\.com|microsoft365\\.com)")),

        // 11 Games
        (11, R("(store\\.steampowered\\.com|steampowered\\.com|epicgames\\.com|gog\\.com|itch\\.io|battle\\.net|roblox\\.com)")),

        // 1 Graphics
        (1, R("(figma\\.com|canva\\.com|photopea\\.com|dribbble\\.com|behance\\.net)")),

        // 24 Web Design
        (24, R("(webflow\\.com|wix\\.com|squarespace\\.com)")),

        // 25 Multimedia
        (25, R("(youtube\\.com|youtu\\.be|netflix\\.com|spotify\\.com|twitch\\.tv|vimeo\\.com|soundcloud\\.com|hulu\\.com|disneyplus\\.com|primevideo\\.com)")),

        // 26 Productivity
        (26, R("(notion\\.so|trello\\.com|asana\\.com|clickup\\.com|todoist\\.com|monday\\.com|linear\\.app|miro\\.com|evernote\\.com)")),

        // 27 Networking & Communication
        (27, R("(reddit\\.com|linkedin\\.com|x\\.com|twitter\\.com|facebook\\.com|instagram\\.com|threads\\.net)")),

        // 29 Reference / documentation / info
        (29, R("(wikipedia\\.org|wiktionary\\.org|britannica\\.com|investopedia\\.com|arxiv\\.org|scholar\\.google\\.com)")),

        // 34 Finance / accounting
        (34, R("(paypal\\.com|stripe\\.com|wise\\.com|revolut\\.com|quickbooks\\.intuit\\.com|xero\\.com|banking|onlinebanking)")),

        // 36 File transfer / sharing
        (36, R("(drive\\.google\\.com|dropbox\\.com|onedrive\\.live\\.com|onedrive\\.com|box\\.com|wetransfer\\.com)"))
    };

    public int? ClassifyAsync(ApplicationRecord record)
    {
        var text = $"{record.ClassName} {record.ProcessName}"
            .ToLowerInvariant();

        foreach (var (category, pattern) in _applicationRules)
        {
            if (pattern.IsMatch(text))
                return category;
        }

        return null;
    }

    public IEnumerable<int?> ClassifyAsync(IEnumerable<ApplicationRecord> records)
    {
        foreach (var r in records)
            yield return ClassifyAsync(r);
    }

    public int? ClassifyAsync(BrowserRecord record)
    {
        var text = BuildBrowserText(record);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var (category, pattern) in _websiteRules)
        {
            if (pattern.IsMatch(text))
                return category;
        }

        return null;
    }

    public IEnumerable<int?> ClassifyAsync(IEnumerable<BrowserRecord> records)
    {
        foreach (var r in records)
            yield return ClassifyAsync(r);
    }

    private static string BuildBrowserText(BrowserRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Url))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(record.Url, UriKind.Absolute, out var uri))
        {
            return record.Url.ToLowerInvariant();
        }

        return $"{uri.Host} {uri.AbsolutePath} {record.Url}".ToLowerInvariant();
    }

    private static Regex R(string pattern) =>
        new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
}
