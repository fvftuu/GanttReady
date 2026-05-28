using Microsoft.Data.Sqlite;
var conn = new SqliteConnection("Data Source=I:\\NetPlan\\src\\NetPlan.Server\\netplan.db"); conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = @"
SELECT tPred.Code AS PredCode, tPred.Name AS Pred,
       tSucc.Code AS SuccCode, tSucc.Name AS Succ
FROM TaskRelations r
JOIN Tasks tPred ON r.PredecessorTaskId = tPred.Id
JOIN Tasks tSucc ON r.SuccessorTaskId = tSucc.Id
JOIN Projects p ON r.ProjectId = p.Id
WHERE p.Code = 'NEV-2024'
ORDER BY tPred.SortOrder";
var rdr = cmd.ExecuteReader();
Console.WriteLine("NEV 任务关系:");
while (rdr.Read())
    Console.WriteLine($"  {rdr["PredCode"],-10} → {rdr["SuccCode"],-10}  ({rdr["Pred"]} → {rdr["Succ"]})");
rdr.Close();

// Check if idx 10 (内外饰) has predecessor
cmd.CommandText = @"
SELECT COUNT(*) FROM TaskRelations r
JOIN Tasks t ON r.SuccessorTaskId = t.Id
JOIN Projects p ON r.ProjectId = p.Id
WHERE p.Code = 'NEV-2024' AND t.Code = 'NEV-01.3.2'";
var hasPred = (long)cmd.ExecuteScalar();
Console.WriteLine($"\n内外饰设计有前置关系: {(hasPred > 0 ? "✅" : "❌ 无前置 = 第0天开始")}");
conn.Close();
