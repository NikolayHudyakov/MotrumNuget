using Motrum.Wpf.DataBase.Config;
using Motrum.Wpf.DataBase.Interfases;
using Npgsql;
using System.Data;
using System.Net.NetworkInformation;

namespace Motrum.Wpf.DataBase
{
    internal class PostgreSql : IDataBase<PostgreSqlConfig>
    {
        private const int PingTimeout = 1000;
        private const int ErrorTimeout = 1000;
        private const int ConnStatusTimeout = 1000;

        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private NpgsqlConnection? _сonnection;
        private readonly object _lockObjExecuteReader = new();
        private bool _connected;

        public PostgreSqlConfig? Dto { get; set; }

        bool IDataBase.Connected => _connected;

        public event Action<bool>? Status;
        public event Action<string>? Error;

        public async Task StartAsync() => await Task.Run(Start);

        public async Task StopAsync() => await Task.Run(Stop);

        public int ExecuteSqlRaw(string sql, params object?[] parameters)
        {
            try
            {
                lock (_lockObjExecuteReader)
                    using (NpgsqlCommand command = _сonnection!.CreateCommand())
                    {
                        var paramNames = Enumerable.Range(0, parameters.Length).Select((i) => $"@{i}").ToArray();

                        command.CommandText = string.Format(sql, paramNames);

                        for (var i = 0; i < paramNames.Length; i++)
                            command.Parameters.AddWithValue(paramNames[i], parameters[i]!);

                        using NpgsqlDataReader dataReader = command.ExecuteReader();
                        return dataReader.RecordsAffected;
                    }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        public DataTable FromSqlRaw(string sql, params object?[] parameters)
        {
            var data = new DataTable();
            try
            {
                lock (_lockObjExecuteReader)
                    using (NpgsqlCommand command = _сonnection!.CreateCommand())
                    {
                        var paramNames = Enumerable.Range(0, parameters.Length).Select((i) => $"@{i}").ToArray();

                        command.CommandText = string.Format(sql, paramNames);

                        for (var i = 0; i < paramNames.Length; i++)
                            command.Parameters.AddWithValue(paramNames[i], parameters[i]!);

                        using NpgsqlDataReader dataReader = command.ExecuteReader();
                        data.Load(dataReader);
                        return data;
                    }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return data;
            }
        }

        private void Start()
        {
            if (!_startStopFlag)
            {
                _startStopFlag = true;
                try
                {
                    _сonnection = new NpgsqlConnection();

                    _сonnection.ConnectionString =
                        $"""
                         Server = {Dto!.Server};
                         Port = {Dto.Port};
                         User Id = {Dto.User};
                         Password = {Dto.Password};
                         Database = {Dto.DataBase}
                         """;
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                }

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start();
            }
        }

        private void Stop()
        {
            if (_startStopFlag)
            {
                _startStopFlag = false;

                _connectionStatusThread?.Join();

                _сonnection?.Close();
                _сonnection?.Dispose();

                Status?.Invoke(false);
            }
        }

        private void ConnectionStatusCycle()
        {
            while (_startStopFlag)
            {
                if (Dto == null)
                    continue;

                if (_connected = Connected())
                {
                    Status?.Invoke(true);
                    Thread.Sleep(ConnStatusTimeout);
                    continue;
                }

                Status?.Invoke(false);

                try
                {
                    _сonnection!.Open();
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private bool Connected()
        {
            try
            {
                using Ping ping = new();
                return ping.Send(Dto!.Server!, PingTimeout).Status == IPStatus.Success &&
                    _сonnection!.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}
