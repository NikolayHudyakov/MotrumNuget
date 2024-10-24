using Motrum.Wpf.DataBase.Config;
using Motrum.Wpf.DataBase.Enums;
using Motrum.Wpf.Services;
using Motrum.Wpf.Services.Config;
using System.Data;

namespace Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var db = new DataBaseService();
            db.Status += Console.WriteLine;
            db.Error += Console.WriteLine;

            db.StartAsync(new DataBaseConfig
            {
                Dbms = DbmsType.MsSql,
                PostgreSql = new DbConfig
                {
                    Server = "localhost",
                    Port = "5432",
                    User = "postgres",
                    Password = "root",
                    DataBase = "marking"
                },
                MsSql = new DbConfig
                {
                    Server = "localhost",
                    Port = "5432",
                    User = "sa",
                    Password = "1",
                    DataBase = "marking"
                }
            }).Wait();

            Thread.Sleep(1000);

            DataTable response = db.FromSqlRaw(
                """
                SELECT * FROM work_line_samara;
                """);

            Console.WriteLine(response.Rows[0]["code"].ToString());


            Console.ReadKey();

            db.StopAsync().Wait();
        }
    }
}
