using Microsoft.Data.Sqlite;

var conn = new SqliteConnection("Data Source=I:\\NetPlan\\src\\NetPlan.Server\\netplan.db");
conn.Open();

// Check ALL projects for extreme small BudgetCost values
var cmd = conn.CreateCommand();
cmd.CommandText = @"
SELECT p.Name, t.Code, t.Name, t.BudgetCost
FROM Tasks t
JOIN Projects p ON t.ProjectId = p.Id
WHERE t.BudgetCost > 0 AND t.BudgetCost < 100
ORDER BY t.BudgetCost";
var rdr = cmd.ExecuteReader();
bool found = false;
while (rdr.Read())
{
    found = true;
    Console.WriteLine($"{rdr["Name"]} | {rdr["Code"]} | {rdr["TaskName"]} | Budget={rdr["BudgetCost"]}");
}
if (!found) Console.WriteLine("NO tasks with BudgetCost between 1 and 100");
conn.Close();
