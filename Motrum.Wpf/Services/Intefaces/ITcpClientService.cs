using Motrum.Wpf.Services.Config;

namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с TCP клиентом
    /// </summary>
    public interface ITcpClientService
    {
        /// <summary>
        /// Возникает один раз в секунду и указывает
        /// статус подключения к TCP серверу
        /// </summary>
        public event Action<bool>? Status;

        /// <summary>
        /// Возникает при появлениии исключения 
        /// в любой операции сервиса
        /// </summary>
        public event Action<string>? Error;

        /// <summary>
        /// Инкапсулирует метод, который обрабативает данные считанные с буфера
        /// </summary>
        /// <param name="data"></param>
        public delegate void DataReceiveEventHandler(string data);

        /// <summary>
        /// Возникает при появлении данных в буфере
        /// </summary>
        public event DataReceiveEventHandler? DataReceive;

        /// <summary>
        /// Статус подключения к TCP серверу
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <param name="config">Настройки сервиса</param>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public Task StartAsync(TcpClientConfig config);

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public Task StopAsync();

        /// <summary>
        /// Отправляет данные на TCP сервер
        /// </summary>
        /// <param name="data">Данные для отправки</param>
        /// <returns>Задача представляющая асинхронную отправку данных на TCP сервер</returns>
        public Task SendDataAsync(string data);
    }
}
