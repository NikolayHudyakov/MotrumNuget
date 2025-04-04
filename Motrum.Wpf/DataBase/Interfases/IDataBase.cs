using System.Data;

namespace Motrum.Wpf.DataBase.Interfases
{
    /// <summary>
    /// Сервис для работы с базой данных<br/>
    /// Следит за подключением к бд
    /// </summary>
    public interface IDataBase<T>
    {
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
        /// Статус подключения к бд
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Выполняет транзакцию состоящую из SQL запросов определеных в callback функции <paramref name="sqlRequestsCallback"/><br/>
        /// Результатом выполнения callback функции должно быть значение true - фиксация изменений или false - откат изменений
        /// </summary>
        /// <param name="sqlRequestsCallback"></param>
        public void ExecuteTransaction(Func<bool> sqlRequestsCallback);

        /// <summary>
        /// Асинхронно запускает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхронный запуск сервиса</returns>
        public Task StartAsync(T config);

        /// <summary>
        /// Асинхронно останавливает сервис
        /// </summary>
        /// <returns>Задача представляющая асинхроную остановку сервиса</returns>
        public Task StopAsync();

        /// <summary>
        /// Выполняет заданный SQL для базы данных и 
        /// возвращает количество затронутых строк
        /// </summary>
        /// <param name="sql">Выполняемый SQL</param>
        /// <param name="parameters">Параметры для использования с SQL</param>
        /// <returns>Число обработанных строк</returns>
        public int ExecuteSqlRaw(string sql, params object?[] parameters);

        /// <summary>
        /// Выполняет заданный SQL для базы данных и 
        /// возвращает данные согласно запросу
        /// </summary>
        /// <param name="sql">Выполняемый SQL</param>
        /// <param name="parameters">Параметры для использования с SQL</param>
        /// <returns>Данные соотетствующие запрсу</returns>
        public DataTable FromSqlRaw(string sql, params object?[] parameters);
    }
}
