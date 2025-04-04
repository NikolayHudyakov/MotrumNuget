using Motrum.Wpf.DataBase.Config;
using Motrum.Wpf.DataBase.Interfases;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using System.Net.NetworkInformation;
using System.Transactions;
using static Azure.Core.HttpHeader;

namespace Motrum.Wpf.DataBase
{
    internal class MySql : IDataBase<DbConfig>
    {
        private const int PingTimeout = 1000;
        private const int ErrorTimeout = 1000;
        private const int ConnStatusTimeout = 1000;

        private bool _startStopFlag;
        private Thread? _connectionStatusThread;
        private MySqlConnection? _сonnection;
        private readonly object _lockObjExecuteReader = new();
        private bool _connected;

        public bool Connected => _connected;

        public event Action<bool>? Status;
        public event Action<string>? Error;

        public async Task StartAsync(DbConfig config) => await Task.Run(() => Start(config));

        public async Task StopAsync() => await Task.Run(Stop);

        public DbTransaction? BeginTransaction()
        {
            try
            {
                if (_сonnection == null)
                    throw new Exception("Сервис не запущен");

                return _сonnection.BeginTransaction();
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return null;
            }
        }

        public int ExecuteSqlRaw(DbTransaction? transaction, string sql, params object?[] parameters)
        {
            try
            {
                if (_сonnection == null)
                    throw new Exception("Сервис не запущен");

                lock (_lockObjExecuteReader)
                    using (MySqlCommand command = _сonnection.CreateCommand())
                    {
                        command.Transaction = transaction as MySqlTransaction;

                        var paramNames = Enumerable.Range(0, parameters.Length).Select((i) => $"@{i}").ToArray();

                        command.CommandText = string.Format(sql, paramNames);

                        for (var i = 0; i < paramNames.Length; i++)
                            command.Parameters.AddWithValue(paramNames[i], parameters[i]!);

                        using MySqlDataReader dataReader = command.ExecuteReader();
                        return dataReader.RecordsAffected;
                    }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return 0;
            }
        }

        public DataTable FromSqlRaw(DbTransaction? transaction, string sql, params object?[] parameters)
        {
            var data = new DataTable();
            try
            {
                if (_сonnection == null)
                    throw new Exception("Сервис не запущен");

                lock (_lockObjExecuteReader)
                    using (MySqlCommand command = _сonnection.CreateCommand())
                    {
                        command.Transaction = transaction as MySqlTransaction;

                        var paramNames = Enumerable.Range(0, parameters.Length).Select((i) => $"@{i}").ToArray();

                        command.CommandText = string.Format(sql, paramNames);

                        for (var i = 0; i < paramNames.Length; i++)
                            command.Parameters.AddWithValue(paramNames[i], parameters[i]!);

                        using MySqlDataReader dataReader = command.ExecuteReader();
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

        private void Start(DbConfig config)
        {
            if (!_startStopFlag)
            {
                _startStopFlag = true;
                try
                {
                    _сonnection = new MySqlConnection();

                    _сonnection.ConnectionString =
                        $"""
                         Server = {config.Server};
                         Port = {config.Port};
                         User Id = {config.User};
                         Password = {config.Password};
                         Database = {config.DataBase}
                         """;
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                }

                _connectionStatusThread = new Thread(ConnectionStatusCycle);
                _connectionStatusThread.Start(config);
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

        private void ConnectionStatusCycle(object? obj)
        {
            if (obj is not DbConfig config)
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

                try
                {
                    _сonnection?.Open();
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex.Message);
                    Thread.Sleep(ErrorTimeout);
                }
            }
        }

        private bool GetConnectionStatus(DbConfig config)
        {
            try
            {
                using Ping ping = new();
                return ping.Send(config.Server, PingTimeout).Status == IPStatus.Success &&
                    _сonnection != null && _сonnection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}
