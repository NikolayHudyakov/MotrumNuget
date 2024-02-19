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
        /// Параметры подключения к базе данных
        /// </summary>
        public PostgreSqlConfig PostgreSql { get; set; } = new();

        /// <summary>
        /// Параметры подключения к базе данных
        /// </summary>
        public MySqlConfig MySql { get; set; } = new();
    }
}
