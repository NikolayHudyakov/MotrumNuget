namespace Motrum.Wpf.Services.Config
{
    /// <summary>
    /// Настройки сервиса для работы с сетевым адаптером Modbus TCP
    /// </summary>
    public class ModbusTcpAdapterConfig
    {
        /// <summary>
        /// IP адрес
        /// </summary>
        public string IPAddress { get; set; } = string.Empty;

        /// <summary>
        /// Порт
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// ID подчиненого устройства
        /// </summary>
        public byte SlaveAddress { get; set; }

        /// <summary>
        /// Начальный адрес регистра для чтения дискретных входов
        /// </summary>
        public ushort DIStartAddress { get; set; }

        /// <summary>
        /// Колличество регистров для чтения дискретных входов
        /// </summary>
        public ushort DINumberOfPoints { get; set; }

        /// <summary>
        /// Начальный адрес регистра для чтения скорости с модуля энкодера
        /// </summary>
        public ushort EncoderStartAddress { get; set; }

        /// <summary>
        /// Колличество регистров для чтения скорости с модуля энкодера
        /// </summary>
        public ushort EncoderNumberOfPoints { get; set; }

        /// <summary>
        /// Период чтения скорости с модуля энкодера
        /// </summary>
        public int EncoderPollingPeriod { get; set; }

    }
}
