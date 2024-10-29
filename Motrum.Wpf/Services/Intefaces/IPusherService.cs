namespace Motrum.Wpf.Services.Intefaces
{
    /// <summary>
    /// Сервис для работы со сталкивателем<br/>
    /// <br/>
    /// В режиме сталкивателя:<br/>
    /// По умолчанию все обьекты сталкивания добавленные с помощью методов
    /// <see cref="PushByDelayAsync"/> и <see cref="PushByDistanceAsync"/>
    /// не будут вызывать callback функцию по истечении временной задержки или по пройденому расстоянию.
    /// Для того чтобы произошел вызов callback функции для конкретного обьекта сталкивания
    /// необходимо вызвать метод <see cref="UpdateAsync"/> 
    /// с указанием ID обьекта, который не должен быть отбракован<br/>
    /// <br/>
    /// В режиме отбраковщика:<br/>
    /// По умолчанию все обьекты отбраковки добавленные с помощью методов
    /// <see cref="PushByDelayAsync"/> и <see cref="PushByDistanceAsync"/>
    /// будут вызывать callback функцию по истечении временной задержки или по пройденому расстоянию.
    /// Для того чтобы отменить вызов callback функции для конкретного обьекта отбраковки
    /// необходимо вызвать метод <see cref="UpdateAsync"/> 
    /// с указанием ID обьекта, который должен быть столкнут
    /// </summary>
    public interface IPusherService
    {
        /// <summary>
        /// Режим отбраковщика - сталкивает обьект, если не был вызван метод <see cref="UpdateAsync(long)"/>
        /// </summary>
        public bool RejecterMode { get; set; }

        /// <summary>
        /// Скорость м/с
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
        /// В режиме сталкивателя:<br/>
        /// Асинхронно делает возможным вызов callback функции
        /// для обьекта сталкивания указанного в параметре <paramref name="id"/><br/>
        /// <br/>
        /// В режиме отбраковщика:<br/>
        /// Асинхронно отменяет вызов callback функции
        /// для обьекта отбраковки указанного в параметре <paramref name="id"/>
        /// </summary>
        /// <param name="id">ID обьекта</param>
        /// <returns>Задача представляющая асинхронную операцию</returns>
        public Task UpdateAsync(long id);
    }
}
