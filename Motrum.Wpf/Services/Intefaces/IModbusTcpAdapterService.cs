using Motrum.Wpf.Services.Config;

namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с сетевым адаптером Modbus TCP
    /// </summary>
    public interface IModbusTcpAdapterService
    {
        /// <summary>
        /// Настройки сервиса
        /// </summary>
        public ModbusTcpAdapterConfig? Config { get; set; }

        /// <summary>
        /// Возникает один раз в секунду и указывает
        /// статус подключения к адаптеру
        /// </summary>
        public event Action<bool>? Status;

        /// <summary>
        /// Возникает при появлениии исключения 
        /// в любой операции сервиса
        /// </summary>
        public event Action<string>? Error;

        /// <summary>
        /// Инкапсулирует метод, который обрабатывает появление переднего или заднего фронт
        /// дискретных входов
        /// </summary>
        /// <param name="leadingEdgeInputs">Передний фронт</param>
        /// <param name="trailingEdgeInputs">Задний фронт</param>
        /// <param name="timeMillisecond">Время чтения дискретных входов</param>
        public delegate void ReadDIEventHendler(bool[] leadingEdgeInputs, bool[] trailingEdgeInputs, double timeMillisecond);

        /// <summary>
        /// Возникает при появлении переднего или заднего
        /// фронта на любом из дискретных входов определенных в <see cref="Config"/>
        /// </summary>
        public event ReadDIEventHendler? ReadDI;

        /// <summary>
        /// Инкапсулирует метод, который обрабатывает данные считанные с модуля энкодера
        /// </summary>
        /// <param name="buffer">Скорость энкодера (импульс/мин)</param>
        public delegate void ReadEncoderEventHendler(ushort[] buffer);

        /// <summary>
        /// Возникает с периодичностью указанной в <see cref="Config"/> 
        /// и представляет скорость энкодера в импульс/мин
        /// </summary>
        public event ReadEncoderEventHendler? ReadEncoder;

        /// <summary>
        /// Статус подключения к адаптеру
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public Task StartAsync();

        /// <summary>
        /// Асинхронно останавливает сервис с указанием последовательности дискретных выходов, 
        /// которые должны перейти в состояние false
        /// </summary>
        /// <param name="startAddressDo">Начальный адрес регистра</param>
        /// <param name="countDo">Количество регистров</param>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public Task StopAsync(ushort startAddressDo, int countDo);

        /// <summary>
        /// Асинхронно записывает значение одного дискретного выхода
        /// </summary>
        /// <param name="coilAddress">Адрес регистра</param>
        /// <param name="value">Значение</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в микросекундах
        /// </returns>
        public Task<double> WriteSingleDOAsync(ushort coilAddress, bool value);

        /// <summary>
        /// Асинхронно записывает значения последовательности дискретных выходов
        /// </summary>
        /// <param name="startAddress">Начальный адрес регистра</param>
        /// <param name="data">Значения</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в микросекундах
        /// </returns>
        public Task<double> WriteMultipleDOAsync(ushort startAddress, bool[] data);
    }
}
