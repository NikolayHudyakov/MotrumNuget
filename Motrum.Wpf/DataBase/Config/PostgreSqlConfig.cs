namespace Motrum.Wpf.DataBase.Config
{
    /// <summary>
    /// Параметры подключения к базе данных PostgreSql
    /// </summary>
    public class PostgreSqlConfig
    {
        /// <summary>
        /// Имя или IP адрес хоста
        /// </summary>
        public string? Server { get; set; }

        /// <summary>
        /// Порт
        /// </summary>
        public string? Port { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string? User { get; set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Имя базы данных
        /// </summary>
        public string? DataBase { get; set; }
    }
}
