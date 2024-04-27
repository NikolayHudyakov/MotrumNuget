using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using static Motrum.Wpf.Services.Intefaces.ITcpClientService;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с TCP клиентом
    /// </summary>
    public class TcpClientService : ITcpClientService
    {
        private const int PingTimeout = 1000;
        private const int ErrorTimeout = 1000;
        private const int ConnStatusTimeout = 1000;

        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private Thread? _receiveThread;
        private TcpClient? _tcpClient;
        private bool _connected;
        private NetworkStream? _networkStream;

        /// <summary>
        /// Настройки сервиса
        /// </summary>
        public TcpClientConfig? Config { get; set; }

        /// <summary>
        /// Статус подключения к TCP серверу
        /// </summary>
        public bool Connected => _connected;

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
        /// Возникает при появлении данных в буфере
        /// </summary>
        public event DataReceiveEventHandler? DataReceive;

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
        /// Отправляет данные на TCP сервер
        /// </summary>
        /// <param name="data">Данные для отправки</param>
        /// <returns>Задача представляющая асинхронную отправку данных на TCP сервер</returns>
        public async Task SendDataAsync(string data)
        {
            if (Config == null || !_connected || _networkStream == null)
                return;

            try
            {
                await _networkStream.WriteAsync(Encoding.UTF8.GetBytes(data)).AsTask();
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                Thread.Sleep(ErrorTimeout);
            }
        }

        private void Start()
        {
            if (!_startStopFlag)
            {
                _startStopFlag = true;

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start();

                _receiveThread = new Thread(ReceiveCycle);
                _receiveThread.Start();
            }
        }

        private void Stop()
        {
            if (_startStopFlag)
            {
                _startStopFlag = false;

                _connectionStatusThread?.Join();
                _tcpClient?.Close();

                Status?.Invoke(false);
                _connected = false;
            }
        }

        private void ConnectionStatusCycle()
        {
            Task taskConnection;

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
                    taskConnection = _tcpClient.ConnectAsync(Config.IPAddress, Config.Port);

                    if (!taskConnection.Wait(ConnStatusTimeout))
                    {
                        throw new TimeoutException();
                    }

                    _networkStream = _tcpClient.GetStream();
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                }
            }
        }

        private void ReceiveCycle()
        {
            byte[]? data = null;

            while (_startStopFlag)
            {
                if (Config == null || !_connected || _tcpClient == null || _networkStream == null)
                {
                    Thread.Sleep(ErrorTimeout);
                    continue;
                }

                data ??= new byte[_tcpClient.ReceiveBufferSize];
                try
                {
                    int bytesCount = _networkStream.Read(data, 0, data.Length);

                    DataReceive?.Invoke(Encoding.UTF8.GetString(data, 0, bytesCount));
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
    }
}
