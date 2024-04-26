using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;
using System.IO.Ports;
using System.Text;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с последовательным портом
    /// </summary>
    public class SerialPortService : ISerialPortService
    {
        private const int ErrorTimeout = 1000;
        private const int ConnStatusTimeout = 1000;
        private const int DataBits = 8;
        private const int ReadTimeOut = 100;

        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private SerialPort? _serialPort;
        private bool _connected;
        /// <summary>
        /// Настройки сервиса
        /// </summary>
        public SerialPortConfig? Config { get; set; }

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public bool Connected => _connected;


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
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public async Task StartAsync() => await Task.Run(Start);

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронную остановку сервиса</returns>
        public async Task StopAsync() => await Task.Run(Stop);

        /// <summary>
        /// Отправляет данные на устройство
        /// </summary>
        /// <param name="data">Данные для отправки</param>
        /// <returns>Задача представляющая асинхронную отправку данных на устройство</returns>
        public Task SendDataAsync(string data)
        {
            throw new NotImplementedException();
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

        private void ConnectionStatusCycle()
        {
            while (_startStopFlag)
            {
                if (Config == null)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                if (_connected = _serialPort != null && _serialPort.IsOpen)
                {
                    Status?.Invoke(true);
                    Thread.Sleep(ConnStatusTimeout);
                    continue;
                }

                Status?.Invoke(false);
                try
                {
                    _serialPort = new SerialPort()
                    {
                        PortName = Config.PortName,
                        BaudRate = Config.Baudrate,
                        Parity = Parity.None,
                        DataBits = DataBits,
                        StopBits = StopBits.One
                    };
                    _serialPort.DataReceived += SerialPortDataReceived;

                    _serialPort.Open();
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            StringBuilder stringBuilder = new();

            Thread.Sleep(ReadTimeOut);

            try
            {
                while (_startStopFlag)
                {
                    if (_serialPort == null)
                        continue;

                    stringBuilder.Append(_serialPort.ReadExisting());

                    if (_serialPort.BytesToRead == 0) break;
                }

                DataReceive?.Invoke(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
            }
        }

        private void Stop()
        {
            if (_startStopFlag)
            {
                _startStopFlag = false;

                _connectionStatusThread?.Join();

                _serialPort?.Close();

                Status?.Invoke(false);
            }
        }
    }
}
