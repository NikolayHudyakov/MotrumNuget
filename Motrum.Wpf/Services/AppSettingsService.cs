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
        protected override string FilePath => Path.Combine(AppContext.BaseDirectory, FileName);
    }
}
