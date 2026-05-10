using Microsoft.Data.Sqlite;

var connStr = "Data Source=I:\\NetPlan\\src\\NetPlan.Server\\bin\\Debug\\net8.0\\netplan.db";
using var conn = new SqliteConnection(connStr);
conn.Open();

var sql = @"SELECT COUNT(*) FROM Tasks";
using var cmd = new SqliteCommand(sql, conn);
Console.WriteLine($"bin/Debug/net8.0 Tasks: {cmd.ExecuteScalar()}");

var sql2 = @"SELECT COUNT(*) FROM Projects";
using var cmd2 = new SqliteCommand(sql2, conn);
Console.WriteLine($"bin/Debug/net8.0 Projects: {cmd2.ExecuteScalar()}");

var sql3 = @"SELECT Id, Name FROM Projects";
using var cmd3 = new SqliteCommand(sql3, conn);
using var r = cmd3.ExecuteReader();
Console.WriteLine("Projects:");
while (r.Read()) Console.WriteLine($"  ID={r[0]} {r[1]}");
