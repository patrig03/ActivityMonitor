using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    public int InsertIntervention(InterventionDto intervention)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "interventions") != 0)
        {
            throw new Exception("Database exception in interventions table");
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO interventions (user_id, category_id, triggered_at, type)
        VALUES ($user, $category, $triggered, $type);
        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$user", intervention.UserId);
        cmd.Parameters.AddWithValue("$category", intervention.CategoryId);
        cmd.Parameters.AddWithValue("$triggered", intervention.TriggeredAt);
        cmd.Parameters.AddWithValue("$type", intervention.Type);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public IEnumerable<InterventionDto> GetInterventionsForUser(int userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        SELECT intervention_id, user_id, category_id, triggered_at, type
        FROM interventions
        WHERE user_id = $user;
        """;

        cmd.Parameters.AddWithValue("$user", userId);

        using var reader = cmd.ExecuteReader();
        var list = new List<InterventionDto>();

        while (reader.Read())
        {
            list.Add(new InterventionDto
            {
                InterventionId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                CategoryId = reader.GetInt32(2),
                TriggeredAt = reader.GetDateTime(3),
                Type = reader.GetString(4),
            });
        }

        return list;
    }

}