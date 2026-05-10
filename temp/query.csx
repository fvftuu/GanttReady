using Microsoft.Data.Sqlite;

var conn = new SqliteConnection("Data Source=I:\\NetPlan\\src\\NetPlan.Server\\Data\\netplan.db");
conn.Open();

Console.WriteLine("=== Tasks (name containing 'han') ===");
using (var cmd = new SqliteCommand("SELECT Id, Code, Name, PlanStartDate, PlanEndDate, PlanDuration, IsManualSchedule, EarlyStart, EarlyFinish, LateStart, LateFinish, TotalFloat, IsCritical FROM Tasks WHERE Name LIKE '%han%' OR Code LIKE '%han%'", conn))
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
    {
        Console.WriteLine($"ID={reader[0]}, Code={reader[1]}, Name={reader[2]}, PlanStart={reader[3]}, PlanEnd={reader[4]}, Dur={reader[5]}, Manual={reader[6]}, ES={reader[7]}, EF={reader[8]}, LS={reader[9]}, LF={reader[10]}, TF={reader[11]}, Crit={reader[12]}");
    }
}

Console.WriteLine("\n=== Relations involving han tasks ===");
using (var cmd = new SqliteCommand(@"SELECT r.Id, r.PredecessorTaskId, p.Code as PredCode, r.Type, r.Lag, r.SuccessorTaskId, s.Code as SuccCode 
FROM TaskRelations r 
LEFT JOIN Tasks p ON r.PredecessorTaskId = p.Id 
LEFT JOIN Tasks s ON r.SuccessorTaskId = s.Id 
WHERE p.Name LIKE '%han%' OR p.Code LIKE '%han%' OR s.Name LIKE '%han%' OR s.Code LIKE '%han%'", conn))
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
    {
        Console.WriteLine($"RelID={reader[0]}, Pred={reader[1]}({reader[2]}), Type={reader[3]}, Lag={reader[4]}, Succ={reader[5]}({reader[6]})");
    }
}

conn.Close();
