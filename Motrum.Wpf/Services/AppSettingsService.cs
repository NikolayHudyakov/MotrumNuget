namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Сервис хранения пользовательских настроек приложения <typeparamref name="T"/> 
    /// </summary>
    public class AppSettingsService<T> : Serializer<T>
    {
        private const string FileName = "appsettings.json";

        /// <summary>
        /// Путь к файлу настроек
        /// </summary>
        protected override string FilePath => CreateBaseFilePath(FileName);

        /// <summary>
        /// Создает путь к файлу базового каталога
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Путь к файлу</returns>
        public static string CreateBaseFilePath(string fileName) => 
            Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
