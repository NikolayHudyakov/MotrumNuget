namespace Motrum.Wpf.Services.Config
{
    /// <summary>
    /// Настройки сервиса для работы с TCP клиентом
    /// </summary>
    public class TcpClientConfig
    {
        /// <summary>
        /// IP адрес
        /// </summary>
        public string IPAddress { get; set; } = string.Empty;

        /// <summary>
        /// Порт
        /// </summary>
        public int Port { get; set; }
    }
}
