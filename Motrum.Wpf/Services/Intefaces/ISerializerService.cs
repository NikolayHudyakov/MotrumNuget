namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис хранения обьекта типа <typeparamref name="T"/>
    /// </summary>
    /// <typeparamref name="T"></typeparamref>
    public interface ISerializerService<T>
    {
        /// <summary>
        /// Считывает json файл и десериализует в обьект типа <typeparamref name="T"/>
        /// </summary>
        /// <returns>Обьект типа <typeparamref name="T"/></returns>
        public T Obj { get; }

        /// <summary>
        /// Сериализует обьект типа <typeparamref name="T"/> в json и сохраняет в файл
        /// </summary>
        public void Serialize(T obj);
    }
}
