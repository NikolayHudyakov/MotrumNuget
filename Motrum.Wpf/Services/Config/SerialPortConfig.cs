namespace Motrum.Wpf.Services.Config
{
    /// <summary>
    /// Настройки сервиса для работы с последовательным портом
    /// </summary>
    public class SerialPortConfig
    {
        /// <summary>
        /// Имя порта
        /// </summary>
        public string? PortName { get; set; }

        /// <summary>
        /// Скорость
        /// </summary>
        public int Baudrate { get; set; }

    }
}
