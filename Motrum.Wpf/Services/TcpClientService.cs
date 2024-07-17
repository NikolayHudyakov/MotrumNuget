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

        private TcpClientConfig? _config;
        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private Thread? _receiveThread;
        private TcpClient? _tcpClient;
        private bool _connected;
        private NetworkStream? _networkStream;

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
        /// <param name="config">Настройки сервиса</param>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public async Task StartAsync(TcpClientConfig config) => await Task.Run(() => Start(config));

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
            if (_config == null || !_connected || _networkStream == null)
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

        private void Start(TcpClientConfig config)
        {
            if (!_startStopFlag)
            {
                _config = config;

                _startStopFlag = true;

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start(config);

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
                _receiveThread?.Join();

                Status?.Invoke(false);
                _connected = false;
            }
        }

        private void ConnectionStatusCycle(object? obj)
        {
            if (obj is not TcpClientConfig config)
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
                    _tcpClient.Connect(config.IPAddress, config.Port);
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
                if (!_connected || _tcpClient == null || _networkStream == null)
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
                    if(_startStopFlag)
                        Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private bool GetConnectionStatus(TcpClientConfig config)
        {
            using Ping ping = new();
            try
            {
                return ping.Send(config.IPAddress, PingTimeout).Status == IPStatus.Success &&
                    _tcpClient != null && _tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
