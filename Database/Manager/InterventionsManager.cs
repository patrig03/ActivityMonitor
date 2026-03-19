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
        INSERT INTO interventions (threshold_id, triggered_at, snoozed)
        VALUES ($threshold_id, $triggered, $snoozed);
        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$threshold_id", intervention.ThresholdId);
        cmd.Parameters.AddWithValue("$triggered", intervention.TriggeredAt);
        cmd.Parameters.AddWithValue("$snoozed", intervention.Snoozed);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public IEnumerable<InterventionDto> GetInterventionsForUser(int userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        SELECT i.intervention_id, i.threshold_id, i.snoozed, i.triggered_at
        FROM interventions i
        LEFT JOIN thresholds t ON t.threshold_id = i.threshold_id
        WHERE t.user_id = $user
        ORDER BY i.triggered_at DESC;
        """;

        cmd.Parameters.AddWithValue("$user", userId);

        using var reader = cmd.ExecuteReader();
        var list = new List<InterventionDto>();

        while (reader.Read())
        {
            list.Add(new InterventionDto
            {
                Id = reader.GetInt32(0),
                ThresholdId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                Snoozed = !reader.IsDBNull(2) && reader.GetBoolean(2),
                TriggeredAt = reader.GetDateTime(3),
            });
        }

        return list;
    }
}
