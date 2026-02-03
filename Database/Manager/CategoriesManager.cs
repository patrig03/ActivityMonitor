using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public void InsertDefaultCategories()
    {
        // Each tuple contains: CategoryId, Name, Description
        var categories = new List<(int Id, string Name, string Desc)>
        {
            (1,  "Graphics",                     "Graphics viewers, editors, graphics demos, screensavers etc."),
            (2,  "Browsers",                     "Netscape, Opera, Mozilla, Mosaic, IE, ..."),
            (3,  "Email, news and Groupware",   "Email and related programs."),
            (4,  "Chat, Instant Messaging, Telephony",
                "Telegram, Teams, Skype, Zoom, ..."),
            (5,  "Programming/Software Engineering",
                "Languages, Compilers, IDEs, CASE tools etc."),
            (6,  "Utilities",                    "Misc. Utilities"),
            (7,  "Scientific/Technical/Math",
                "Scientific and mathematic applications"),
            (8,  "File System",                  "File System Utilities (e.g. CD writer stuff, file managers, shells, ...)"),
            (9,  "Office Suites",                "Productivity apps that contain bundles of applications."),
            (10, "CAD/CAE",                      "Computer Aided Design, Computer Aided Engineering"),
            (11, "Games",                        "Games"),

            // Sub‑categories
            (12, "Sound Editing",
                "Sound editing suites, recorders, mixing and sampling."),
            (13, "Audio Players",
                "MP3, WAV, and other format audio players."),

            (14, "Graphics Viewer",  "Image viewing"),
            (15, "Graphics Editing", "Image editing/vector drawing software"),
            (16, "Animation/Rendering/3D",
                "Image animation for multimedia or web"),

            (17, "Audio",   "Audio related applications"),
            (18, "Video",   "Video players, editors and codecs"),

            (19, "Compression",  "Compression Tools"),
            (20, "Word Processing", "Type, edit, print, OCR!"),
            (21, "Spreadsheet",    "Do it yourself Number crunching"),
            (22, "Database",       "Relational Database"),
            (23, "Presentation",   "Slide Shows with animation and sound and flowchart tools"),
            (24, "Web Design",     "Create your own web page"),
            (25, "Multimedia",     "Graphics, Audio and Video"),

            (26, "Productivity",          "Productivity applications"),
            (27, "Networking & Communication",
                "Network, Internet related programs and comm stuff"),
            (28, "Net Tools",
                "Tools such as proxies, web crawlers, search engines, ..."),

            (29, "Reference/Documentation/Info",
                "Encyclopedias, information resources, data tracking, ..."),
            (30, "EDA/Measurement",
                "Electronics design tools, measurement and stuff"),
            (31, "Mathematics",
                "Mathematical and Statistical software."),
            (32, "Text Editors",
                "Multipurpose text editing tool. No formatting just text."),

            (33, "Office Utilities",
                "Misc Office tools that usually work in conjunction with other Office software."),
            (34, "Finance/Accounting/Project/CRM",
                "Personal and Business Finance Software and project planning, CRM (Customer Relationship Management)."),
            (35, "Flowchart/Diagraming/graphs",
                "Software to design flowcharts and diagrams"),

            (36, "File transfer/sharing",
                "FTP, NFS, document sharing, Samba, scp, ..."),
            (37, "Installers",
                "Program installers like installshield, windows installer etc."),
            (38, "Remote Access",
                "SSH, Telnet, VNC, Terminal Services, ..."),

            // Educational / Games
            (39, "Educational games / children",
                "Games that encourage learning."),
            (40, "Card, Puzzle and Board Games",
                "Card Games, mind puzzles and \"traditional\" stuff."),
            (41, "Educational Software, CBT",
                "Educational tools, Computer Based Training"),
            (42, "Action Games",
                "Arcade and platform action games"),
            (43, "Sports Games",
                "Professional sports, car racing, and more."),
            (44, "Simulation Games",
                "Flight and other real life simulators."),
            (45, "Adventures",
                "Graphical Adventure Games"),
            (46, "Online (MMORPG) Games",
                "Massively Multiplayer Online Role Playing Games."),
            (47, "1st Person Shooter",
                "Games such as Doom, Quake, Half-Life."),
            (48, "Role Playing Games",
                "Games where you build up your characters through battle and experience."),
            (49, "Strategy Games",
                "Build your army, conquer the world"),
            (50, "Game Tools",
                "Misc. tools related to games."),
            (51, "Emulators",
                "Software that emulates game hardware."),

            // Misc
            (52, "Desktop Publishing",
                "Various page layout, print, and publishing applications."),
            (53, "Astronomy",
                "An endless (almost) empty space... ;-)")
        };

        foreach (var (id, name, desc) in categories)
        {
            var dto = new CategoryDto
            {
                CategoryId = id,
                Name       = name,
                Description= desc
            };
            InsertCategory(dto);
        }
    }

    public int InsertCategory(CategoryDto c)
    {
        if (DatabaseValidator.VerifyTable(_connection.CreateCommand(), "categories") != 0)
        {
            throw new Exception("Database exception in categories table");
        }        
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO categories (name, description)
            VALUES (@name, @description);
            """;
    
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@description", c.Description);

        // Execute the command to insert the new category
        cmd.ExecuteNonQuery();

        // Now, retrieve the last inserted row ID using a separate query
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt32(selectCmd.ExecuteScalar());
    }



    public CategoryDto? GetCategory(int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM categories WHERE category_id = $id";
        cmd.Parameters.AddWithValue("$id", categoryId);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new CategoryDto
        {
            CategoryId = r.GetInt32(0),
            Name = r.GetString(1),
            Description = r.GetString(2)
        };
    }

    public IEnumerable<CategoryDto> GetAllCategories()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM categories";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new CategoryDto
            {
                CategoryId = r.GetInt32(0),
                Name = r.GetString(1),
                Description = r.GetString(2)
            };
        }
    }
}