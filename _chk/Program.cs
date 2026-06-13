var dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
var dbPath = System.IO.Path.Combine(dir, @"..\..\..\..\src\GanttReady.Server\ganttready.db");
dbPath = System.IO.Path.GetFullPath(dbPath);
System.Console.Error.WriteLine("DB: " + dbPath);
if (!System.IO.File.Exists(dbPath)) { System.Console.Error.WriteLine("NOT FOUND"); return; }
using var db = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + dbPath);
db.Open();
var c = db.CreateCommand();
c.CommandText = "SELECT COUNT(*) FROM Projects";
System.Console.WriteLine("Projects: " + c.ExecuteScalar());
c.CommandText = "SELECT COUNT(*) FROM Tasks";
System.Console.WriteLine("Tasks: " + c.ExecuteScalar());
c.CommandText = "SELECT COUNT(*) FROM TaskRelations";
System.Console.WriteLine("Relations: " + c.ExecuteScalar());
c.CommandText = "SELECT Id,Code,Name FROM Tasks LIMIT 5";
var r = c.ExecuteReader();
while (r.Read()) System.Console.WriteLine($"  Task: Id={r[0]} Code={r[1]} Name={r[2]}");
