using Motrum.Wpf.DataBase;
using Motrum.Wpf.DataBase.Enums;
using Motrum.Wpf.DataBase.Interfases;
using Motrum.Wpf.Services.Config;
using Motrum.Wpf.Services.Intefaces;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис для работы с базами данных <br/>
    /// Предоставляет обьект для работы с конретной базой данных
    /// </summary>
    public class DataBaseService : IDataBaseService
    {
        private readonly IDataBase? _dataBase;

        /// <summary>
        /// Обьект для работы с конретной базой данных определенной в
        /// настройках <see cref="DataBaseConfig"/>
        /// </summary>
        public IDataBase DataBase => _dataBase ?? GetDataBase();

        /// <summary>
        /// Настройки для работы с базой данных
        /// </summary>
        public DataBaseConfig Config { get; set; } = new();

        private IDataBase GetDataBase()
        {
            return Config.Dbms switch
            {
                DbmsType.PostgreSql => new PostgreSql() { Dto = Config.PostgreSql },
                DbmsType.MySql => new DataBase.MySql() { Dto = Config.MySql },
                _ => throw new InvalidOperationException("СУБД не поддерживается"),
            };
        }
    }
}
