using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    public int InsertIntervention(InterventionDto intervention)
    {
        TableValidator.EnsureTableExists(_connection,
            "interventions",
            "intervention_id",
            "user_id",
            "category_id",
            "session_id",
            "triggered_at",
            "type",
            "intensity"
        );

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO interventions (user_id, category_id, session_id, triggered_at, type, intensity)
        VALUES ($user, $category, $session, $triggered, $type, $intensity);
        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$user", intervention.UserId);
        cmd.Parameters.AddWithValue("$category", intervention.CategoryId);
        cmd.Parameters.AddWithValue("$session", intervention.SessionId);
        cmd.Parameters.AddWithValue("$triggered", intervention.TriggeredAt);
        cmd.Parameters.AddWithValue("$type", intervention.Type);
        cmd.Parameters.AddWithValue("$intensity", intervention.Intensity);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public IEnumerable<InterventionDto> GetInterventionsForUser(int userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        SELECT intervention_id, user_id, category_id, session_id, triggered_at, type, intensity
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
                SessionId = reader.GetInt32(3),
                TriggeredAt = reader.GetDateTime(4),
                Type = reader.GetString(5),
                Intensity = reader.GetInt32(6)
            });
        }

        return list;
    }

}