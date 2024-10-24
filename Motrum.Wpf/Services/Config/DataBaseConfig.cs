using Motrum.Wpf.DataBase.Config;
using Motrum.Wpf.DataBase.Enums;

namespace Motrum.Wpf.Services.Config
{
    /// <summary>
    /// Настройки для работы с базами данных
    /// </summary>
    public class DataBaseConfig
    {
        /// <summary>
        /// Текущая СУБД
        /// </summary>
        public DbmsType Dbms { get; set; }

        /// <summary>
        /// Параметры подключения к PostgreSql
        /// </summary>
        public DbConfig PostgreSql { get; set; } = new();

        /// <summary>
        /// Параметры подключения к MySql
        /// </summary>
        public DbConfig MySql { get; set; } = new();

        /// <summary>
        /// Параметры подключения к MsSql
        /// </summary>
        public DbConfig MsSql { get; set; } = new();
    }
}
