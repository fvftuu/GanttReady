using Microsoft.Data.Sqlite;
var db = new SqliteConnection("Data Source=src/NetPlan.Server/netplan.db");
db.Open();
var cmd = db.CreateCommand();
cmd.CommandText = "SELECT Id, Code, Name, PlanStartDate, PlanEndDate, WorkDayBits, WorkdaysPerWeek FROM Projects WHERE Id=1";
var r = cmd.ExecuteReader();
while (r.Read())
{
    Console.WriteLine($"ID={r[0]}: {r[1]} {r[2]}");
    Console.WriteLine($"  Start={r[3]}, End={r[4]}");
    Console.WriteLine($"  WorkDayBits={r[5]}, WorkdaysPerWeek={r[6]}");
}
