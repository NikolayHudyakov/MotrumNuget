using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;
using NModbus;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с Modbus TCP клиентом
    /// </summary>
    public class ModbusTcpClientService : IModbusTcpClientService
    {
        private const int PingTimeout = 1000;
        private const int ErrorTimeout = 1000;
        private const int ConnStatusTimeout = 1000;

        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private TcpClient? _tcpClient;
        private IModbusMaster? _modbusMaster;
        private Task? _writeMultipleDoTask;
        private Task? _writeSingleDoTask;
        private Task<bool[]>? _readMultipleDiTask;
        private bool _connected;

        /// <summary>
        /// Статус подключения клиента к серверу
        /// </summary>
        public bool Connected => _connected;

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
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public async Task StartAsync() =>
            await Task.Run(Start);

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public async Task StopAsync() =>
            await Task.Run(Stop);

        /// <summary>
        /// Асинхронно считывает значения группы дискретных входов
        /// </summary>
        /// <param name="startAddress">Начальный адрес регистра</param>
        /// <param name="numberOfPoints">Колличество регистров</param>
        /// <returns>
        /// Задача представляющая асинхронное чтение,
        /// результатом которой является кортэж состоящий из масива входов и время потраченое на чтение в милисекундах
        /// </returns>
        public async Task<(bool[] inputs, double timeMillisecond)> ReadMultipleDiAsync(ushort startAddress, ushort numberOfPoints)
        {
            try
            {
                if (Config == null || !_connected || _modbusMaster == null)
                    throw new Exception("Не заданы настройки сервиса или ошибка подключения");

                var dateTime = DateTime.Now;

                bool[] inputs = await(_readMultipleDiTask = _modbusMaster.ReadInputsAsync(Config.SlaveAddress, startAddress, numberOfPoints));

                return (inputs, (DateTime.Now - dateTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return ([], 0);
            }
        }

        /// <summary>
        /// Асинхронно записывает значение одного дискретного выхода
        /// </summary>
        /// <param name="coilAddress">Адрес регистра</param>
        /// <param name="value">Значение</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в милисекундах
        /// </returns>
        public async Task<double> WriteSingleDoAsync(ushort coilAddress, bool value)
        {
            if (Config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeSingleDoTask = _modbusMaster.WriteSingleCoilAsync(Config.SlaveAddress, coilAddress, value));
                return (DateTime.Now - dateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Асинхронно записывает значения последовательности дискретных выходов
        /// </summary>
        /// <param name="startAddress">Начальный адрес регистра</param>
        /// <param name="data">Значения</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в милисекундах
        /// </returns>
        public async Task<double> WriteMultipleDoAsync(ushort startAddress, bool[] data)
        {
            if (Config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeMultipleDoTask = _modbusMaster.WriteMultipleCoilsAsync(Config.SlaveAddress, startAddress, data));
                return (DateTime.Now - dateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        private void Start()
        {
            if (!_startStopFlag)
            {
                _startStopFlag = true;

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start();
            }
        }

        private void Stop()
        {
            if (_startStopFlag)
            {
                _startStopFlag = false;

                _writeSingleDoTask?.Wait();
                _writeMultipleDoTask?.Wait();
                _connectionStatusThread?.Join();

                _tcpClient?.Close();

                Status?.Invoke(false);
                _connected = false;
            }
        }

        private void ConnectionStatusCycle()
        {
            while (_startStopFlag)
            {
                if (Config == null)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                if (_connected = GetConnectionStatus())
                {
                    Status?.Invoke(true);
                    Thread.Sleep(ConnStatusTimeout);
                    continue;
                }

                Status?.Invoke(false);

                _tcpClient?.Close();
                _tcpClient = new TcpClient();

                try
                {
                    _tcpClient.Connect(Config.IpAddress, Config.Port);
                    _modbusMaster = new ModbusFactory().CreateMaster(_tcpClient);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                }
            }
        }

        private bool GetConnectionStatus()
        {
            if (Config == null)
                return false;

            using Ping ping = new();
            try
            {
                return ping.Send(Config.IpAddress, PingTimeout).Status == IPStatus.Success &&
                    _tcpClient != null && _tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
