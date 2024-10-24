using Motrum.Wpf.DataBase;
using Motrum.Wpf.DataBase.Config;
using Motrum.Wpf.DataBase.Enums;
using Motrum.Wpf.DataBase.Interfases;
using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;
using System.Data;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с базами данных <br/>
    /// Предоставляет обьект для работы с конретной базой данных
    /// </summary>
    public class DataBaseService : IDataBaseService
    {
        private IDataBase<DbConfig>? _database;
        private bool _serviceStarted;

        /// <summary>
        /// Статус подключения к бд
        /// </summary>
        public bool Connected => _database != null && _database.Connected;

        /// <summary>
        /// Возникает один раз в секунду и указывает
        /// статус подключения к бд
        /// </summary>
        public event Action<bool>? Status;

        /// <summary>
        /// Возникает при появлениии исключения во время 
        /// выполнения какой либо операции с бд
        /// </summary>
        public event Action<string>? Error;

        /// <summary>
        /// Выполняет заданный SQL для базы данных и 
        /// возвращает количество затронутых строк
        /// </summary>
        /// <param name="sql">Выполняемый SQL</param>
        /// <param name="parameters">Параметры для использования с SQL</param>
        /// <returns>Число обработанных строк</returns>
        public int ExecuteSqlRaw(string sql, params object?[] parameters) =>
            _database != null ? _database.ExecuteSqlRaw(sql, parameters) : 0;

        /// <summary>
        /// Выполняет заданный SQL для базы данных и 
        /// возвращает данные согласно запросу
        /// </summary>
        /// <param name="sql">Выполняемый SQL</param>
        /// <param name="parameters">Параметры для использования с SQL</param>
        /// <returns>Данные соотетствующие запрсу</returns>
        public DataTable FromSqlRaw(string sql, params object?[] parameters) =>
            _database != null ? _database.FromSqlRaw(sql, parameters) : new DataTable();

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public async Task StartAsync(DataBaseConfig config)
        {
            if (_serviceStarted) return;

            _serviceStarted = true;

            switch (config.Dbms)
            {
                case DbmsType.PostgreSql:
                    _database = new PostgreSql();
                    _database.Status += InvokeStatus;
                    _database.Error += InvokeError;
                    await _database.StartAsync(config.PostgreSql);
                    break;

                case DbmsType.MySql:
                    _database = new DataBase.MySql();
                    _database.Status += InvokeStatus;
                    _database.Error += InvokeError;
                    await _database.StartAsync(config.MySql);
                    break;

                case DbmsType.MsSql:
                    _database = new MsSql();
                    _database.Status += InvokeStatus;
                    _database.Error += InvokeError;
                    await _database.StartAsync(config.MsSql);
                    break;

                default:
                    throw new InvalidOperationException("СУБД не поддерживается");
            }
        }

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхроную остановку сервиса</returns>
        public async Task StopAsync()
        {
            if(_database != null)
            {
                await _database.StopAsync();

                _database.Status -= InvokeStatus;
                _database.Error -= InvokeError;
            }
               
            _serviceStarted = false;
        }

        private void InvokeStatus(bool status) => Status?.Invoke(status);

        private void InvokeError(string error) => Error?.Invoke(error);
    }
}
