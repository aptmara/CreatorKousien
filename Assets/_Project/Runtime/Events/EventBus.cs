// 制作者: 山内陽
using System;
using System.Collections.Generic;

namespace Game.Core.Events
{
    /// <summary>
    /// 軽量EventBus実装。
    /// UIや各機能間の連携（一方向通信）に利用する。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var existing))
            {
                _subscribers[type] = Delegate.Combine(existing, handler);
            }
            else
            {
                _subscribers[type] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var existing))
            {
                var newDelegate = Delegate.Remove(existing, handler);
                if (newDelegate == null)
                {
                    _subscribers.Remove(type);
                }
                else
                {
                    _subscribers[type] = newDelegate;
                }
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var existing))
            {
                var handler = existing as Action<T>;
                handler?.Invoke(eventData);
            }
        }

        public static void ClearAll()
        {
            _subscribers.Clear();
        }
    }
}
