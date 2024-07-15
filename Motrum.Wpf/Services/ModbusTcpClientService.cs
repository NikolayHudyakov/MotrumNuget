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

        private ModbusTcpClientConfig? _config;
        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private TcpClient? _tcpClient;
        private IModbusMaster? _modbusMaster;
        private Task? _writeMultipleDoTask;
        private Task? _writeSingleDoTask;
        private Task? _writeSingleAoTask;
        private Task<bool[]>? _readMultipleDiTask;
        private bool _connected;

        /// <summary>
        /// Статус подключения клиента к серверу
        /// </summary>
        public bool Connected => _connected;

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
        /// <param name="config">Настройки сервиса</param>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public async Task StartAsync(ModbusTcpClientConfig config) =>
            await Task.Run(() => Start(config));

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
                if (_config == null || _modbusMaster == null)
                    throw new Exception("Не заданы настройки сервиса или ошибка подключения");

                var dateTime = DateTime.Now;

                bool[] inputs = await(_readMultipleDiTask = _modbusMaster.ReadInputsAsync(_config.SlaveAddress, startAddress, numberOfPoints));

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
            if (_config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeSingleDoTask = _modbusMaster.WriteSingleCoilAsync(_config.SlaveAddress, coilAddress, value));
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
            if (_config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeMultipleDoTask = _modbusMaster.WriteMultipleCoilsAsync(_config.SlaveAddress, startAddress, data));
                return (DateTime.Now - dateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Асинхронно записывает значение на аналоговый выход
        /// </summary>
        /// <param name="registerAddress">Адрес регистра</param>
        /// <param name="value">Значение</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в милисекундах
        /// </returns>
        public async Task<double> WriteSingleAoAsync(ushort registerAddress, ushort value)
        {
            if (_config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeSingleAoTask = _modbusMaster.WriteSingleRegisterAsync(_config.SlaveAddress, registerAddress, value));
                return (DateTime.Now - dateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        private void Start(ModbusTcpClientConfig config)
        {
            if (!_startStopFlag)
            {
                _config = config;

                _startStopFlag = true;

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start(config);
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

        private void ConnectionStatusCycle(object? obj)
        {
            if (obj is not ModbusTcpClientConfig config)
                return;

            while (_startStopFlag)
            {
                if (_connected = GetConnectionStatus(config))
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
                    _tcpClient.Connect(config.IpAddress, config.Port);
                    _modbusMaster = new ModbusFactory().CreateMaster(_tcpClient);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                }
            }
        }

        private bool GetConnectionStatus(ModbusTcpClientConfig config)
        {
            using Ping ping = new();
            try
            {
                return ping.Send(config.IpAddress, PingTimeout).Status == IPStatus.Success &&
                    _tcpClient != null && _tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
