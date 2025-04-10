﻿using Motrum.Wpf.DataBase.Config;
using Motrum.Wpf.DataBase.Enums;
using Motrum.Wpf.Services;
using Motrum.Wpf.Services.Config;
using System.Data;
using System.Data.Common;

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
                Dbms = DbmsType.MySql,
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
                },
                MySql = new DbConfig
                {
                    Server = "localhost",
                    Port = "3306",
                    User = "root",
                    Password = "root",
                    DataBase = "dm_code"
                }
            }).Wait();

            Thread.Sleep(1000);

           
            

            //transaction?.Rollback();

           


            Console.ReadKey();

            db.StopAsync().Wait();
        }
    }
}
