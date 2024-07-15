using Motrum.Wpf.DataBase.Interfases;
using Motrum.Wpf.Services.Config;

namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с базами данных <br/>
    /// Предоставляет обьект для работы с конретной базой данных
    /// </summary>
    public interface IDataBaseService
    {
        /// <summary>
        /// Возвращает конретную базу данных определеную в
        /// настройках <see cref="DataBaseConfig"/>
        /// </summary>
        /// <param name="config"></param>
        /// <returns>Обьект базы данных</returns>
        public IDataBase GetDataBase(DataBaseConfig config);
    }
}
