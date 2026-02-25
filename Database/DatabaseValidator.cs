using Microsoft.Data.Sqlite;

namespace Database;

public class DatabaseValidator : IDatabaseValidator
{
    private const string UsersTable = """
                                      CREATE TABLE IF NOT EXISTS users (
                                          user_id INTEGER PRIMARY KEY,
                                          display_name TEXT,
                                          pin_hash TEXT,
                                          sync_enabled INTEGER,
                                          created_at DATETIME
                                      );
                                      """;
    private const string SettingsTable = """
                                         CREATE TABLE IF NOT EXISTS settings (
                                             settings_id INTEGER PRIMARY KEY,
                                             user_id INTEGER,
                                             focus_mode_enabled INTEGER,
                                             FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
                                         );
                                         """;
    private const string CategoriesTable = """
                                           CREATE TABLE IF NOT EXISTS categories (
                                               category_id INTEGER PRIMARY KEY,
                                               name TEXT,
                                               description TEXT
                                           );
                                           """;
    private const string ApplicationsTable = """
                                             CREATE TABLE IF NOT EXISTS applications (
                                                 app_id INTEGER PRIMARY KEY,
                                                 category_id INTEGER,
                                                 name TEXT,
                                                 class TEXT,
                                                 process_name TEXT,
                                                 type TEXT,
                                                 FOREIGN KEY (category_id) REFERENCES categories(category_id)
                                             );
                                             """;
    private const string SessionsTable = """
                                         CREATE TABLE IF NOT EXISTS sessions (
                                             session_id INTEGER PRIMARY KEY,
                                             app_id INTEGER,
                                             user_id INTEGER,
                                             start_time DATETIME,
                                             end_time DATETIME,
                                             duration_sec INTEGER,
                                             FOREIGN KEY (app_id) REFERENCES applications(app_id),
                                             FOREIGN KEY (user_id) REFERENCES users(user_id)
                                         );
                                         """;
    private const string BrowserActivityTable = """
                                                CREATE TABLE IF NOT EXISTS browser_activity (
                                                    activity_id INTEGER PRIMARY KEY,
                                                    user_id INTEGER,
                                                    app_id INTEGER,
                                                    url TEXT,
                                                    title TEXT,
                                                    FOREIGN KEY (user_id) REFERENCES users(user_id),
                                                    FOREIGN KEY (app_id) REFERENCES applications(app_id)
                                                );
                                                """;
    private const string ThresholdsTable = """
                                           CREATE TABLE IF NOT EXISTS thresholds (
                                               threshold_id INTEGER PRIMARY KEY,
                                               user_id INTEGER,
                                               category_id INTEGER,
                                               daily_limit_sec INTEGER,
                                               weekly_limit_sec INTEGER,
                                               FOREIGN KEY (user_id) REFERENCES users(user_id),
                                               FOREIGN KEY (category_id) REFERENCES categories(category_id)
                                           );
                                           """;
    private const string InterventionsTable = """
                                              CREATE TABLE IF NOT EXISTS interventions (
                                                  intervention_id INTEGER PRIMARY KEY,
                                                  user_id INTEGER,
                                                  category_id INTEGER,
                                                  session_id INTEGER,
                                                  triggered_at DATETIME,
                                                  type TEXT,
                                                  FOREIGN KEY (user_id) REFERENCES users(user_id),
                                                  FOREIGN KEY (category_id) REFERENCES categories(category_id),
                                                  FOREIGN KEY (session_id) REFERENCES sessions(session_id)
                                              );
                                              """;

    private const string CategoriesDefaultPopulation = """
                                                       INSERT INTO categories (category_id, name, description) VALUES
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
                                                       
                                                       (39, "Educational games / children",
                                                           "Games that encourage learning."),
                                                       (40, "Card, Puzzle and Board Games",
                                                           "Card Games, mind puzzles and other stuff."),
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
                                                       
                                                       (52, "Desktop Publishing",
                                                           "Various page layout, print, and publishing applications."),
                                                       (53, "Astronomy",
                                                           "An endless (almost) empty space... ;-)")
                                                       """;
    
    public void EnsureDatabase(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        CreateTable(cmd, "users");
        CreateTable(cmd, "settings");
        CreateTable(cmd, "categories");
        CreateTable(cmd, "applications");
        CreateTable(cmd, "sessions");
        CreateTable(cmd, "browser_activity");
        CreateTable(cmd, "thresholds");
        CreateTable(cmd, "interventions");
        
        PopulateTableDefaults(cmd, "categories");
    }
    private void CreateTable(SqliteCommand cmd, string tableName)
    {
        var tableCode = GetCodeFromTableName(tableName);
        cmd.CommandText = tableCode;
        cmd.ExecuteNonQuery();
    }
    
    private void PopulateTableDefaults(SqliteCommand cmd, string tableName)
    {
        // Check if the table already contains rows before inserting defaults.
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count > 0) return;
        
        var code = GetPopulationsCode(tableName);
        cmd.CommandText = code;
        cmd.ExecuteNonQuery();
    }
    
    /// Verifies that the table contains all expected columns and no extra columns are present
    /// returns -1 if there are columns missing, 1 if there are extra columns, and 0 if is equal
    public int VerifyTable(SqliteCommand cmd, string tableName)
    {
        var tableCode = GetCodeFromTableName(tableName);
        var requiredColumns = ExtractColumnNames(tableCode);
        
        cmd.CommandText = $"PRAGMA table_info({tableName});";

        var found = new HashSet<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            found.Add(r.GetString(1));
        }

        // Check if all required columns are present
        foreach (var col in requiredColumns)
        {
            if (!found.Contains(col))
            {
                return -1;
            }
        }

        // Ensure there are no extra columns beyond those specified
        var extraColumns = found.Except(requiredColumns);
        return extraColumns.Any() ? 1 : 0;
    }
    
    /// <summary>
    /// Extracts column names from the CREATE TABLE command.
    /// </summary>
    /// <param name="createTableCommand"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string[] ExtractColumnNames(string createTableCommand)
    {
        if (string.IsNullOrWhiteSpace(createTableCommand))
            return Array.Empty<string>();

        // Find the text inside the first pair of parentheses.
        var start = createTableCommand.IndexOf('(');
        var end   = createTableCommand.LastIndexOf(')');
        if (start < 0 || end <= start)
            throw new ArgumentException("Invalid CREATE TABLE syntax.", nameof(createTableCommand));

        var insideParens = createTableCommand.Substring(start + 1, end - start - 1);

        // Split by commas – each part represents a column definition or a constraint.
        var parts = insideParens.Split(',');

        var columns = new List<string>();

        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            // Skip table constraints such as FOREIGN KEY, UNIQUE, CHECK, etc.
            if (trimmed.StartsWith("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)  ||
                trimmed.StartsWith("UNIQUE",      StringComparison.OrdinalIgnoreCase)  ||
                trimmed.StartsWith("CHECK",       StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // The first token is the column name.
            var tokens = trimmed.Split(
                new char[] { ' ', '\t' }, 
                StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 0)
                columns.Add(tokens[0]);
        }

        return columns.ToArray();
    }

    /// <summary>
    /// Returns the SQL code for the table with the given name
    /// </summary>
    /// <param>
    ///     <name>tableName</name>
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static string GetCodeFromTableName(string tableName)
    {
        return tableName switch
        {
            "users" => UsersTable,
            "settings" => SettingsTable,
            "categories" => CategoriesTable,
            "applications" => ApplicationsTable,
            "sessions" => SessionsTable,
            "browser_activity" => BrowserActivityTable,
            "thresholds" => ThresholdsTable,
            "interventions" => InterventionsTable,
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, null)
        };
    }

    /// <summary>
    /// Returns the SQL code for populating the table with the given name
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static string GetPopulationsCode(string tableName)
    {
        return tableName switch
        {
            "categories" => CategoriesDefaultPopulation,
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, null)
        };
    }
}
