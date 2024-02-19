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
        /// Настройки для работы с базой данных
        /// </summary>
        public DataBaseConfig Config { get; set; }

        /// <summary>
        /// Обьект для работы с конретной базой данных определенной в
        /// настройках <see cref="DataBaseConfig"/>
        /// </summary>
        public IDataBase DataBase { get; }
    }
}
