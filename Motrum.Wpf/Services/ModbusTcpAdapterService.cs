using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;
using NModbus;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static Motrum.Wpf.Services.Intefaces.IModbusTcpAdapterService;

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
        public ModbusTcpAdapterConfig? Config { get; set; }

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
        /// Асинхронно записывает значение одного дискретного выхода
        /// </summary>
        /// <param name="coilAddress">Адрес регистра</param>
        /// <param name="value">Значение</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в микросекундах
        /// </returns>
        public async Task<double> WriteSingleDoAsync(ushort coilAddress, bool value)
        {
            if (Config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeSingleDoTask = _modbusMaster.WriteSingleCoilAsync(Config.SlaveAddress, coilAddress, value));
                return (DateTime.Now - dateTime).TotalMicroseconds;
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
        /// результатом которой является время потраченое на запись в микросекундах
        /// </returns>
        public async Task<double> WriteMultipleDoAsync(ushort startAddress, bool[] data)
        {
            if (Config == null || !_connected || _modbusMaster == null)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await (_writeMultipleDoTask = _modbusMaster.WriteMultipleCoilsAsync(Config.SlaveAddress, startAddress, data));
                return (DateTime.Now - dateTime).TotalMicroseconds;
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

                _readDiThread = new Thread(ReadDiCycle);
                _readDiThread.Start();

                _readEncoderThread = new Thread(ReadEncoderCycle);
                _readEncoderThread.Start();
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
                    _tcpClient.Connect(Config.IPAddress, Config.Port);
                    _modbusMaster = new ModbusFactory().CreateMaster(_tcpClient);

                    _numberOfDi = GetNumberOfDi(_modbusMaster, Config.SlaveAddress);
                    _numberOfDo = GetNumberOfDo(_modbusMaster, Config.SlaveAddress);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private void ReadDiCycle()
        {
            var changeFlag = false;
            DateTime dateTime;
            bool[]? previousInputs = null;
            bool[]? leadingEdgeInputs = null;
            bool[]? trailingEdgeInputs = null;
            bool[] inputs;

            while (_startStopFlag)
            {
                if (Config == null || !_connected || _numberOfDi == 0 || _modbusMaster == null)
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
                    inputs = _modbusMaster.ReadInputs(Config.SlaveAddress, StartAddressDi, _numberOfDi);

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

        private void ReadEncoderCycle()
        {
            ushort[] inputRegisters;

            while (_startStopFlag)
            {
                if (Config == null || !_connected || Config.EncoderNumberOfPoints == 0 || _modbusMaster == null)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                try
                {
                    inputRegisters = _modbusMaster.ReadInputRegisters(Config.SlaveAddress, Config.EncoderStartAddress, Config.EncoderNumberOfPoints);

                    ReadEncoder?.Invoke(inputRegisters);

                    Thread.Sleep(Config.EncoderPollingPeriod);
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
            if (Config == null) 
                return false;

            using Ping ping = new();
            try
            {
                return ping.Send(Config.IPAddress, PingTimeout).Status == IPStatus.Success &&
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
