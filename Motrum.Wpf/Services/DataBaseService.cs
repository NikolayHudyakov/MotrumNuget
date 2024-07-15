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
        /// <summary>
        /// Возвращает конретную базу данных определеную в
        /// настройках <see cref="DataBaseConfig"/>
        /// </summary>
        /// <param name="сonfig"></param>
        /// <returns>Обьект базы данных</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IDataBase GetDataBase(DataBaseConfig сonfig)
        {
            return сonfig.Dbms switch
            {
                DbmsType.PostgreSql => new PostgreSql() { Dto = сonfig.PostgreSql },
                DbmsType.MySql => new DataBase.MySql() { Dto = сonfig.MySql },
                _ => throw new InvalidOperationException("СУБД не поддерживается"),
            };
        }
    }
}
