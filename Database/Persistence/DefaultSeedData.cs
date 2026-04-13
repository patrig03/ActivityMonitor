namespace Database.Persistence;

internal static class DefaultSeedData
{
    public static IReadOnlyList<CategoryEntity> Categories { get; } =
    [
        new() { CategoryId = 1, Name = "Graphics", Description = "Graphics viewers, editors, graphics demos, screensavers etc." },
        new() { CategoryId = 2, Name = "Browsers", Description = "Netscape, Opera, Mozilla, Mosaic, IE, ..." },
        new() { CategoryId = 3, Name = "Email, news and Groupware", Description = "Email and related programs." },
        new() { CategoryId = 4, Name = "Chat, Instant Messaging, Telephony", Description = "Telegram, Teams, Skype, Zoom, ..." },
        new() { CategoryId = 5, Name = "Programming/Software Engineering", Description = "Languages, Compilers, IDEs, CASE tools etc." },
        new() { CategoryId = 6, Name = "Utilities", Description = "Misc. Utilities" },
        new() { CategoryId = 7, Name = "Scientific/Technical/Math", Description = "Scientific and mathematic applications" },
        new() { CategoryId = 8, Name = "File System", Description = "File System Utilities (e.g. CD writer stuff, file managers, shells, ...)" },
        new() { CategoryId = 9, Name = "Office Suites", Description = "Productivity apps that contain bundles of applications." },
        new() { CategoryId = 10, Name = "CAD/CAE", Description = "Computer Aided Design, Computer Aided Engineering" },
        new() { CategoryId = 11, Name = "Games", Description = "Games" },
        new() { CategoryId = 19, Name = "Compression", Description = "Compression Tools" },
        new() { CategoryId = 24, Name = "Web Design", Description = "Create your own web page" },
        new() { CategoryId = 25, Name = "Multimedia", Description = "Graphics, Audio and Video" },
        new() { CategoryId = 26, Name = "Productivity", Description = "Productivity applications" },
        new() { CategoryId = 27, Name = "Networking & Communication", Description = "Network, Internet related programs and comm stuff" },
        new() { CategoryId = 29, Name = "Reference/Documentation/Info", Description = "Encyclopedias, information resources, data tracking, ..." },
        new() { CategoryId = 30, Name = "EDA/Measurement", Description = "Electronics design tools, measurement and stuff" },
        new() { CategoryId = 32, Name = "Text Editors", Description = "Multipurpose text editing tool. No formatting just text." },
        new() { CategoryId = 34, Name = "Finance/Accounting/Project/CRM", Description = "Personal and Business Finance Software and project planning, CRM (Customer Relationship Management)." },
        new() { CategoryId = 35, Name = "Flowchart/Diagraming/graphs", Description = "Software to design flowcharts and diagrams" },
        new() { CategoryId = 36, Name = "File transfer/sharing", Description = "FTP, NFS, document sharing, Samba, scp, ..." },
        new() { CategoryId = 37, Name = "Installers", Description = "Program installers like installshield, windows installer etc." },
        new() { CategoryId = 38, Name = "Remote Access", Description = "SSH, Telnet, VNC, Terminal Services, ..." },
        new() { CategoryId = 51, Name = "Emulators", Description = "Software that emulates game hardware." },
        new() { CategoryId = 52, Name = "Desktop Publishing", Description = "Various page layout, print, and publishing applications." },
    ];
}
