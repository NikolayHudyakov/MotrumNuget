using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;
using NModbus;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static Motrum.Wpf.Services.Intefaces.IModbusTcpAdapterService;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с сетевым адаптером Modbus TCP
    /// </summary>
    public class ModbusTcpAdapterService : IModbusTcpAdapterService
    {
        private const int PingTimeout = 1000;
        private const int ErrorTimeout = 1000;
        private const int ConnStatusTimeout = 1000;
        private const int StartAddressDo = 0;
        private const int StartAddressDi = 0;

        private ModbusTcpAdapterConfig? _config;
        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private Thread? _readDiThread;
        private Thread? _readEncoderThread;
        private TcpClient? _tcpClient;
        private IModbusMaster? _modbusMaster;
        private Task? _writeMultipleDoTask;
        private Task? _writeSingleDoTask;
        private bool _connected;
        private ushort _numberOfDi;
        private ushort _numberOfDo;

        /// <summary>
        /// Настройки сервиса
        /// </summary>

        /// <summary>
        /// Статус подключения к адаптеру
        /// </summary>
        public bool Connected => _connected;

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
        /// Возникает при появлении переднего или заднего
        /// фронта на любом из дискретных входов определенных в <see cref="Config"/>
        /// </summary>
        public event ReadDiEventHendler? ReadDi;

        /// <summary>
        /// Возникает с периодичностью указанной в <see cref="Config"/> 
        /// и представляет скорость энкодера
        /// </summary>
        public event ReadEncoderEventHendler? ReadEncoder;

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <param name="config">Настройки сервиса</param>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public async Task StartAsync(ModbusTcpAdapterConfig config) =>
            await Task.Run(() => Start(config));

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public async Task StopAsync() =>
            await Task.Run(Stop);

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

        private void Start(ModbusTcpAdapterConfig config)
        {
            if (!_startStopFlag)
            {
                _config = config;

                _startStopFlag = true;

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start(config);

                _readDiThread = new Thread(ReadDiCycle);
                _readDiThread.Start(config);

                _readEncoderThread = new Thread(ReadEncoderCycle);
                _readEncoderThread.Start(config);
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
                _readDiThread?.Join();
                _readEncoderThread?.Join();

                WriteMultipleDoAsync(StartAddressDo, new bool[_numberOfDo]).Wait();

                _tcpClient?.Close();

                Status?.Invoke(false);
                _connected = false;
            }
        }

        private void ConnectionStatusCycle(object? obj)
        {
            if (obj is not ModbusTcpAdapterConfig config)
                return;

            while (_startStopFlag)
            {
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
                    _tcpClient.Connect(config.IPAddress, config.Port);
                    _modbusMaster = new ModbusFactory().CreateMaster(_tcpClient);

                    _numberOfDi = GetNumberOfDi(_modbusMaster, config.SlaveAddress);
                    _numberOfDo = GetNumberOfDo(_modbusMaster, config.SlaveAddress);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                }
            }
        }

        private void ReadDiCycle(object? obj)
        {
            if (obj is not ModbusTcpAdapterConfig config)
                return;

            var changeFlag = false;
            DateTime dateTime;
            bool[]? previousInputs = null;
            bool[]? leadingEdgeInputs = null;
            bool[]? trailingEdgeInputs = null;
            bool[] inputs;

            while (_startStopFlag)
            {
                if (!_connected || _numberOfDi == 0 || _modbusMaster == null)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                dateTime = DateTime.Now;
                previousInputs ??= new bool[_numberOfDi];
                leadingEdgeInputs ??= new bool[_numberOfDi];
                trailingEdgeInputs ??= new bool[_numberOfDi];
                try
                {
                    inputs = _modbusMaster.ReadInputs(config.SlaveAddress, StartAddressDi, _numberOfDi);

                    for (int i = 0; i < _numberOfDi; i++)
                    {
                        leadingEdgeInputs[i] = false;
                        trailingEdgeInputs[i] = false;
                        if (inputs![i] != previousInputs[i])
                        {
                            if (inputs![i])
                                leadingEdgeInputs[i] = true;
                            else
                                trailingEdgeInputs[i] = true;
                            changeFlag = true;
                        }
                        previousInputs[i] = inputs![i];
                    }
                    if (changeFlag)
                    {
                        ReadDi?.Invoke(leadingEdgeInputs, trailingEdgeInputs, (DateTime.Now - dateTime).TotalMilliseconds);
                        changeFlag = false;
                    }
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }

                try
                {
                    _writeSingleDoTask?.Wait();
                    _writeMultipleDoTask?.Wait();
                }
                catch { }
            }
        }

        private void ReadEncoderCycle(object? obj)
        {
            if (obj is not ModbusTcpAdapterConfig config)
                return;

            ushort[] inputRegisters;

            while (_startStopFlag)
            {
                if (!_connected || config.EncoderNumberOfPoints == 0 || _modbusMaster == null)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                try
                {
                    inputRegisters = _modbusMaster.ReadInputRegisters(config.SlaveAddress, config.EncoderStartAddress, config.EncoderNumberOfPoints);

                    ReadEncoder?.Invoke(inputRegisters);

                    Thread.Sleep(config.EncoderPollingPeriod);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private bool GetConnectionStatus()
        {
            if (_config == null) 
                return false;

            using Ping ping = new();
            try
            {
                return ping.Send(_config.IPAddress, PingTimeout).Status == IPStatus.Success &&
                    _tcpClient != null && _tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        private ushort GetNumberOfDi(IModbusMaster modbusMaster, byte slaveAddress)
        {
            const ushort NumberOfPoints = 1;

            for (ushort currentInput = 0; true; currentInput++)
            {
                try
                {
                    modbusMaster.ReadInputs(slaveAddress, currentInput, NumberOfPoints);
                }
                catch (SlaveException)
                {
                    return currentInput;
                }
            }
        }

        private ushort GetNumberOfDo(IModbusMaster modbusMaster, byte slaveAddress)
        {
            const ushort NumberOfPoints = 1;

            for (ushort currentCoil = 0; true; currentCoil++)
            {
                try
                {
                    modbusMaster.ReadCoils(slaveAddress, currentCoil, NumberOfPoints);
                }
                catch (SlaveException)
                {
                    return currentCoil;
                }
            } 
        }
    }
}
