using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertCategory(CategoryDto c)
    {
        TableValidator.EnsureTableExists(_connection,
            "categories", 
            "category_id",
            "name"
        );

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO categories (name)
            VALUES (@name);
            """;
    
        cmd.Parameters.AddWithValue("@name", c.Name);

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
            };
        }
    }
}