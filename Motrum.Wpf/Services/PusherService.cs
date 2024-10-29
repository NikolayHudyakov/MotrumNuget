using Motrum.Wpf.Services.Intefaces;

namespace Motrum.Wpf.Services
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
    public class PusherService : IPusherService
    {
        private const int MaxQuantityProduct = 1000;
        private const int VelocityCalcPeriod = 1;

        private readonly object _lockObject = new();

        private readonly List<Item> _list;

        /// <summary>
        /// 
        /// </summary>
        public PusherService() => _list = new List<Item>() { Capacity = MaxQuantityProduct };

        /// <summary>
        /// Режим отбраковщика - сталкивает обьект, если не был вызван метод <see cref="UpdateAsync(long)"/>
        /// </summary>
        public bool RejecterMode {  get; set; }

        /// <summary>
        /// Скорость м/с
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// Асинхронно совершает вызов callback функции <paramref name="pushCallBack"/> 
        /// по истечении времени указанного в параметре <paramref name="delay"/>
        /// </summary>
        /// <param name="id">ID обьекта</param>
        /// <param name="delay">Задержка</param>
        /// <param name="isEnable">Флаг работы отбраковщика</param>
        /// <param name="pushCallBack">Callback функция</param>
        /// <returns>Задача представляющая асинхронный вызов callback функции</returns>
        public async Task PushByDelayAsync(long id, int delay, bool isEnable, Action pushCallBack)
        {
            lock (_lockObject) _list.Add(new Item() { Id = id, Flag = RejecterMode, IsEnable = isEnable });

            await Task.Delay(delay);

            lock (_lockObject)
            {
                Item? item = _list.Find(item => item.Id == id);

                if (item!.Flag && item.IsEnable) Task.Run(pushCallBack);

                _list.Remove(item);
            }
        }

        /// <summary>
        /// Асинхронно совершает вызов callback функции <paramref name="pushCallBack"/> 
        /// по пройденому расстоянию указанному в параметре <paramref name="distance"/>
        /// </summary>
        /// <param name="id">ID обьекта</param>
        /// <param name="distance">Расстояние</param>
        /// <param name="isEnable">Флаг работы отбраковщика</param>
        /// <param name="pushCallBack">Callback функция</param>
        /// <returns>Задача представляющая асинхронный вызов callback функции</returns>
        public async Task PushByDistanceAsync(long id, double distance, bool isEnable, Action pushCallBack)
        {
            lock (_lockObject) _list.Add(new Item() { Id = id, Flag = RejecterMode, IsEnable = isEnable });

            await Task.Run(async () =>
            {
                DateTime previosTime = DateTime.Now;
                double previosVelocity = Velocity;

                while (true)
                {
                    var time = DateTime.Now;
                    double velocity = Velocity;

                    var deltaS = 0.5 * (previosVelocity + velocity) * (time - previosTime).TotalSeconds;

                    previosVelocity = velocity;
                    previosTime = time;

                    distance -= deltaS;
                    if (distance < 0) break;

                    await Task.Delay(VelocityCalcPeriod);
                }
            });

            lock (_lockObject)
            {
                Item? item = _list.Find(item => item.Id == id);

                if (item!.Flag && item.IsEnable) Task.Run(pushCallBack);

                _list.Remove(item);
            }
        }

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
        public async Task UpdateAsync(long id)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    Item? item = _list.Find(item => item.Id == id);

                    if (item != null) item.Flag = !RejecterMode;
                }
            });
        }

        private class Item
        {
            public long Id { get; set; }
            public bool Flag { get; set; }
            public bool IsEnable { get; set; }
        }
    }
}
