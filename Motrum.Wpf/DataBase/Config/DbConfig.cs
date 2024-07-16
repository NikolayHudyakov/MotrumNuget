namespace Motrum.Wpf.DataBase.Config
{
    /// <summary>
    /// Параметры подключения к базе данных
    /// </summary>
    public class DbConfig
    {
        /// <summary>
        /// Имя или IP адрес хоста
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// Порт
        /// </summary>
        public string Port { get; set; } = string.Empty;

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Имя базы данных
        /// </summary>
        public string DataBase { get; set; } = string.Empty;
    }
}
