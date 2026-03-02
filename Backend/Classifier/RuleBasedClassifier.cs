using System.Text.RegularExpressions;
using Backend.DataCollector.Models;

namespace Backend.Classifier;

public class RuleBasedClassifier : IClassifier
{
    private readonly List<(int Category, Regex Pattern)> _rules = new()
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

    public int? ClassifyAsync(ApplicationRecord record)
    {
        var text = $"{record.ClassName} {record.ProcessName}"
            .ToLowerInvariant();

        foreach (var (category, pattern) in _rules)
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

    private static Regex R(string pattern) =>
        new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
}