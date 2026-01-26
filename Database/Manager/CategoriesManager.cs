using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertCategory(CategoryDto c)
    {
        TableValidator.EnsureTableExists(_connection,
            "categories", 
            "category_id",
            "name",
            "confidence"
        );
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO categories (name, confidence)
            VALUES ($name, $conf);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$name", c.Name);
        cmd.Parameters.AddWithValue("$conf", c.Confidence);

        return Convert.ToInt32(cmd.ExecuteScalar());
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
            Confidence = r.GetDecimal(2)
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
                Confidence = r.GetDecimal(2)
            };
        }
    }
}