using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
    public interface IListPool
    {
        void Release(object list);
    }

    public readonly struct ListRentScope : IDisposable
    {
        readonly IListPool _listPool;
        readonly object _list;

        public ListRentScope(IListPool listPool, object list)
        {
            _listPool = listPool;
            _list = list;
        }

        public void Dispose()
        {
            _listPool.Release(_list);
        }
    }

    public class ListPool<TComponent> : IListPool
    {
        readonly List<List<TComponent>> _buffer = new();
        int _availableCount;

        public ListRentScope Rent(out List<TComponent> buffer)
        {
            if (_availableCount == 0)
            {
                buffer = new List<TComponent>();
                return new ListRentScope(this, buffer);
            }

            var index = --_availableCount;
            buffer = _buffer[index];
            _buffer.RemoveAt(index);
            return new ListRentScope(this, buffer);
        }

        public void Release(List<TComponent> buf)
        {
            buf.Clear();
            _buffer.Add(buf);
            ++_availableCount;
            Assert.IsTrue(_buffer.Count < 100, "Too many buffers allocated");
        }

        void IListPool.Release(object list) => Release((List<TComponent>) list);
    }

    public static class ListPools
    {
        public static readonly ListPool<MonoBehaviour> MonoBehaviours = new();
        public static readonly ListPool<Playable> Playable = new();
        public static readonly ListPool<PlayableDirector> PlayableDirector = new();
        public static readonly ListPool<ParticleSystem> ParticleSystem = new();
    }
}