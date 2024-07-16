using Motrum.Wpf.DataBase.Interfases;
using Motrum.Wpf.Services.Config;

namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с базой данных<br/>
    /// Следит за подключением к бд
    /// </summary>
    public interface IDataBaseService : IDataBase<DataBaseConfig>
    {
    }
}
