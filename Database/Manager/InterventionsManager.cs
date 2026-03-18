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


        cmd.Parameters.AddWithValue("$triggered", intervention.TriggeredAt);

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

                TriggeredAt = reader.GetDateTime(3),
            });
        }

        return list;
    }

}