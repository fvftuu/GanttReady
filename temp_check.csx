#r "I:\NetPlan\src\NetPlan.Server\bin\Debug\net8.0\Microsoft.Data.Sqlite.dll"
using Microsoft.Data.Sqlite;
var conn = new SqliteConnection("Data Source=I:\\NetPlan\\src\\NetPlan.Server\\netplan.db");
conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT Code, Name, BudgetCost FROM Tasks WHERE ProjectId=6 ORDER BY Code";
var rdr = cmd.ExecuteReader();
while (rdr.Read()) {
    Console.WriteLine($"{rdr["Code"]} | {rdr["Name"]} | {rdr["BudgetCost"]}");
}
conn.Close();
