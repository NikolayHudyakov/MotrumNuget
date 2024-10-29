namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы с отбраковщиком<br/>
    /// По умолчанию все обьекты отбраковки добавленные с помощью методами 
    /// <see cref="PushByDelayAsync"/> и <see cref="PushByDistanceAsync"/>
    /// будут вызывать callback функцию по истечении временной задержки или по пройденому расстоянию.
    /// Для того чтобы отменить вызов callback функции для конкретного обьекта отбраковки
    /// необходимо вызвать метод <see cref="UpdateAsync"/> 
    /// с указанием ID обьекта, который не должен быть отбракован
    /// </summary>
    public interface IPusherService
    {
        /// <summary>
        /// Скорость (м/с)
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// Асинхронно совершает вызов callback функции <paramref name="rejectCallBack"/> 
        /// по истечении времени указанного в параметре <paramref name="delay"/>
        /// </summary>
        /// <param name="id">ID обьекта</param>
        /// <param name="delay">Задержка</param>
        /// <param name="isEnable">Флаг работы отбраковщика</param>
        /// <param name="rejectCallBack">Callback функция</param>
        /// <returns>Задача представляющая асинхронный вызов callback функции</returns>
        public Task PushByDelayAsync(long id, int delay, bool isEnable, Action rejectCallBack);

        /// <summary>
        /// Асинхронно совершает вызов callback функции <paramref name="rejectCallBack"/> 
        /// по пройденому расстоянию указанному в параметре <paramref name="distance"/>
        /// </summary>
        /// <param name="id">ID обьекта</param>
        /// <param name="distance">Расстояние</param>
        /// <param name="isEnable">Флаг работы отбраковщика</param>
        /// <param name="rejectCallBack">Callback функция</param>
        /// <returns>Задача представляющая асинхронный вызов callback функции</returns>
        public Task PushByDistanceAsync(long id, double distance, bool isEnable, Action rejectCallBack);

        /// <summary>
        /// Асинхронно отменяет вызов callback функции
        /// для обьекта отбраковки указанного в параметре <paramref name="id"/>
        /// </summary>
        /// <param name="id">ID обьекта</param>
        /// <returns>Задача представляющая асинхронную отмену вызова callback функции</returns>
        public Task UpdateAsync(long id);
    }
}
