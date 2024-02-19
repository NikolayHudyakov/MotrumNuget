using Motrum.Wpf.Services.Config;

namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с последовательным портом
    /// </summary>
    public interface ISerialPortService
    {
        /// <summary>
        /// Настройки сервиса
        /// </summary>
        public SerialPortConfig? Config { get; set; }

        /// <summary>
        /// Возникает один раз в секунду и указывает
        /// статус подключения к устройству
        /// </summary>
        public event Action<bool>? Status;

        /// <summary>
        /// Возникает при появлениии исключения 
        /// в любой операции сервиса
        /// </summary>
        public event Action<string>? Error;

        /// <summary>
        /// Возникает при появлении данных в буфере
        /// </summary>
        public event Action<string>? DataReceive;

        /// <summary>
        /// Статус подключения к устройству
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public Task StartAsync();

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public Task StopAsync();

        /// <summary>
        /// Отправляет данные на устройство
        /// </summary>
        /// <param name="data">Данные для отправки</param>
        /// <returns>Задача представляющая асинхронную отправку данных на устройство</returns>
        public Task SendDataAsync(string data);
    }
}
