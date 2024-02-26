using Motrum.Wpf.Services.Intefaces;
using Newtonsoft.Json;

namespace Motrum.Wpf.Services
{
    /// <summary>
    /// Реализует механизм сериализации и десериализации обьекта
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Serializer<T> : ISerializerService<T>
    {
        private T? _obj;

        /// <summary>
        /// Путь к файлу
        /// </summary>
        protected abstract string FilePath { get; }

        /// <summary>
        /// Десериализованный обьект
        /// </summary>
        public T Obj => _obj ??= GetObject();

        private T GetObject()
        {
            string json = string.Empty;

            if (File.Exists(FilePath))
                json = File.ReadAllText(FilePath);

            return JsonConvert.DeserializeObject<T>(json) ?? (T)Activator.CreateInstance(typeof(T))!;
        }

        /// <summary>
        /// Сериализует обьект и сохраняет его по пути указанному в 
        /// <see cref="FilePath"/>
        /// </summary>
        /// <param name="obj">Сериализуемый обьект</param>
        public void Serialize(T obj) => File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
    }
}
