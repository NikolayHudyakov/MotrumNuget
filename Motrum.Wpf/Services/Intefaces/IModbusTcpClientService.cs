using Motrum.Wpf.Services.Config;

namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с Modbus TCP клиентом
    /// </summary>
    public interface IModbusTcpClientService
    {
        /// <summary>
        /// Настройки сервиса
        /// </summary>
        public ModbusTcpClientConfig? Config { get; set; }

        /// <summary>
        /// Возникает один раз в секунду и указывает
        /// статус подключения клиента к серверу
        /// </summary>
        public event Action<bool>? Status;

        /// <summary>
        /// Возникает при появлениии исключения 
        /// в любой операции сервиса
        /// </summary>
        public event Action<string>? Error;

        /// <summary>
        /// Статус подключения клиента к серверу
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
        /// Асинхронно считывает значения группы дискретных входов
        /// </summary>
        /// <param name="startAddress">Начальный адрес регистра</param>
        /// <param name="numberOfPoints">Колличество регистров</param>
        /// <returns>
        /// Задача представляющая асинхронное чтение,
        /// результатом которой является кортэж состоящий из масива входов и время потраченое на чтение в милисекундах
        /// </returns>
        public Task<(bool[] inputs, double timeMillisecond)> ReadMultipleDiAsync(ushort startAddress, ushort numberOfPoints);

        /// <summary>
        /// Асинхронно записывает значение одного дискретного выхода
        /// </summary>
        /// <param name="coilAddress">Адрес регистра</param>
        /// <param name="value">Значение</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в милисекундах
        /// </returns>
        public Task<double> WriteSingleDoAsync(ushort coilAddress, bool value);

        /// <summary>
        /// Асинхронно записывает значения последовательности дискретных выходов
        /// </summary>
        /// <param name="startAddress">Начальный адрес регистра</param>
        /// <param name="data">Значения</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в милисекундах
        /// </returns>
        public Task<double> WriteMultipleDoAsync(ushort startAddress, bool[] data);
    }
}
