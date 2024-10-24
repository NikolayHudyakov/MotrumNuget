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

    /// <summary>
    /// Дискретные входы
    /// </summary>
    public enum DigitalInput : ushort
    {
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        Di0, Di1, Di2, Di3, Di4, Di5, Di6, Di7, Di8, Di9, Di10, Di11, Di12, Di13, Di14, Di15
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Дискретные выходы
    /// </summary>
    public enum DigitalOutput : ushort
    {
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        Do0, Do1, Do2, Do3, Do4, Do5, Do6, Do7, Do8, Do9, Do10, Do11, Do12, Do13, Do14, Do15
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
    }
}
