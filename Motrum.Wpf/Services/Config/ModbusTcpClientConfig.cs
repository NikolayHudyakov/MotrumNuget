namespace Motrum.Wpf.Services.Config
{
    /// <summary>
    /// Настройки Modbus TCP клиента
    /// </summary>
    public class ModbusTcpClientConfig
    {
        /// <summary>
        /// IP адрес
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Порт
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// ID подчиненого устройства
        /// </summary>
        public byte SlaveAddress { get; set; }
    }
}
