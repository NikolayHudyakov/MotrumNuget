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

        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private Thread? _readDIThread;
        private Thread? _readEncoderThread;
        private TcpClient? _tcpClient;
        private IModbusMaster? _modbusMaster;
        private Task<double>? _writeMultipleDOTask;
        private Task<double>? _writeSingleDOTask;
        private bool _connected;

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
        public event ReadDIEventHendler? ReadDI;

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
        /// Асинхронно останавливает сервис с указанием последовательности дискретных выходов, 
        /// которые должны перейти в состояние false
        /// </summary>
        /// <param name="startAddressDo">Начальный адрес регистра</param>
        /// <param name="countDo">Количество регистров</param>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public async Task StopAsync(ushort startAddressDo, int countDo) =>
            await Task.Run(() => Stop(startAddressDo, countDo));


        /// <summary>
        /// Асинхронно записывает значение одного дискретного выхода
        /// </summary>
        /// <param name="coilAddress">Адрес регистра</param>
        /// <param name="value">Значение</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в микросекундах
        /// </returns>
        public async Task<double> WriteSingleDOAsync(ushort coilAddress, bool value) =>
            await (_writeSingleDOTask = _WriteSingleDOAsync(coilAddress, value));

        /// <summary>
        /// Асинхронно записывает значения последовательности дискретных выходов
        /// </summary>
        /// <param name="startAddress">Начальный адрес регистра</param>
        /// <param name="data">Значения</param>
        /// <returns>
        /// Задача представляющая асинхронную запись,
        /// результатом которой является время потраченое на запись в микросекундах
        /// </returns>
        public async Task<double> WriteMultipleDOAsync(ushort startAddress, bool[] data) =>
            await (_writeMultipleDOTask = _WriteMultipleDOAsync(startAddress, data));

        private void Start()
        {
            if (!_startStopFlag)
            {
                _startStopFlag = true;

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start();

                _readDIThread = new Thread(ReadDICycle);
                _readDIThread.Start();

                _readEncoderThread = new Thread(ReadEncoderCycle);
                _readEncoderThread.Start();
            }
        }

        private void Stop(ushort startAddressDo, int countDo)
        {
            if (_startStopFlag)
            {
                _startStopFlag = false;

                _writeSingleDOTask?.Wait();
                _writeMultipleDOTask?.Wait();
                _connectionStatusThread?.Join();
                _readDIThread?.Join();
                _readEncoderThread?.Join();

                _WriteMultipleDOAsync(startAddressDo, new bool[countDo]).Wait();

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

                if (_connected = TcpClientConnected())
                {
                    Status?.Invoke(true);
                    Thread.Sleep(ConnStatusTimeout);
                    continue;
                }

                Status?.Invoke(false);
                _tcpClient = new TcpClient();
                try
                {
                    _tcpClient.Connect(Config.IPAddress, Config.Port);
                    _modbusMaster = new ModbusFactory().CreateMaster(_tcpClient);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    _tcpClient?.Close();
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private void ReadDICycle()
        {
            var changeFlag = false;
            DateTime dateTime;
            bool[]? _previousInputs = null;
            bool[]? _leadingEdgeInputs = null;
            bool[]? _trailingEdgeInputs = null;
            bool[]? inputs;

            while (_startStopFlag)
            {
                if (Config == null || !_connected || Config.DINumberOfPoints == 0)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                dateTime = DateTime.Now;
                _previousInputs ??= new bool[Config.DINumberOfPoints];
                _leadingEdgeInputs ??= new bool[Config.DINumberOfPoints];
                _trailingEdgeInputs ??= new bool[Config.DINumberOfPoints];
                try
                {
                    inputs = _modbusMaster?.ReadInputs(Config.SlaveAddress, Config.DIStartAddress, Config.DINumberOfPoints);

                    for (int i = 0; i < Config.DINumberOfPoints; i++)
                    {
                        _leadingEdgeInputs[i] = false;
                        _trailingEdgeInputs[i] = false;
                        if (inputs![i] != _previousInputs[i])
                        {
                            if (inputs![i])
                                _leadingEdgeInputs[i] = true;
                            else
                                _trailingEdgeInputs[i] = true;
                            changeFlag = true;
                        }
                        _previousInputs[i] = inputs![i];
                    }
                    if (changeFlag)
                    {
                        ReadDI?.Invoke(_leadingEdgeInputs, _trailingEdgeInputs, (DateTime.Now - dateTime).TotalMilliseconds);
                        changeFlag = false;
                    }
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
                _writeSingleDOTask?.Wait();
                _writeMultipleDOTask?.Wait();
            }
        }

        private void ReadEncoderCycle()
        {
            ushort[]? inputRegisters;

            while (_startStopFlag)
            {
                if (Config == null || !_connected || Config.EncoderNumberOfPoints == 0)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                try
                {
                    inputRegisters = _modbusMaster?.ReadInputRegisters(Config.SlaveAddress, Config.EncoderStartAddress, Config.EncoderNumberOfPoints);

                    ReadEncoder?.Invoke(inputRegisters!);

                    Thread.Sleep(Config.EncoderPollingPeriod);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private async Task<double> _WriteSingleDOAsync(ushort coilAddress, bool value)
        {
            if (Config == null || !_connected)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await _modbusMaster!.WriteSingleCoilAsync(Config.SlaveAddress, coilAddress, value);
                return (DateTime.Now - dateTime).TotalMicroseconds;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        private async Task<double> _WriteMultipleDOAsync(ushort startAddress, bool[] data)
        {
            if (Config == null || !_connected)
                return 0;

            try
            {
                var dateTime = DateTime.Now;
                await _modbusMaster!.WriteMultipleCoilsAsync(Config.SlaveAddress, startAddress, data);
                return (DateTime.Now - dateTime).TotalMicroseconds;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        private bool TcpClientConnected()
        {
            if (Config == null) return false;

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
    }
}
