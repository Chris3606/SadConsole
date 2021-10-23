using System;
using System.Collections.Generic;

namespace SadConsole
{
    public enum ChangeType
    {
        Add,
        Replace,
        Insert,
        RemoveItem,
        RemoveIndex,
        Clear,
        Sort
    };

    public readonly struct ListChange<T>
    {
        public readonly ChangeType ChangeType;

        public readonly int Index;
        public readonly T Item;

        public readonly Comparison<T> Comparison;

        public ListChange(ChangeType changeType, int index, T item, Comparison<T> comparison = null)
        {
            ChangeType = changeType;
            Index = index;
            Item = item;
            Comparison = comparison;
        }


        public void ApplyToList(List<T> list)
        {
            switch (ChangeType)
            {
                case ChangeType.Add:
                    list.Add(Item);
                    break;
                case ChangeType.Replace:
                    list[Index] = Item;
                    break;
                case ChangeType.Insert:
                    list.Insert(Index, Item);
                    break;
                case ChangeType.RemoveItem:
                    list.Remove(Item);
                    break;
                case ChangeType.RemoveIndex:
                    list.RemoveAt(Index);
                    break;
                case ChangeType.Clear:
                    list.Clear();
                    break;
                case ChangeType.Sort:
                    if (Comparison == null)
                        list.Sort();
                    else
                        list.Sort(Comparison);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid change type found in change set: {ChangeType}");
            }
        }
    }

    /// <summary>
    /// A wrapper around a list designed to support efficient modification-while-iteration via an change-set caching system, at the expense of additional memory usage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChangeDelayedList<T>
    {
        private List<T> _items;
        public List<T> Items => _items;

        private List<T> _cachedItems;
        public List<T> CachedItems => _cachedItems;

        private List<ListChange<T>> _changes;

        public ChangeDelayedList()
        {
            _items = new List<T>();
            _cachedItems = new List<T>();
            _changes = new List<ListChange<T>>();
        }

        public ChangeDelayedList(IEnumerable<T> collection)
        {
            _items = new List<T>(collection);
            _cachedItems = new List<T>(_items);
            _changes = new List<ListChange<T>>();
        }

        public ChangeDelayedList(int capacity)
        {
            _items = new List<T>(capacity);
            _cachedItems = new List<T>(capacity);
            _changes = new List<ListChange<T>>();
        }

        public void Add(T item)
        {
            _items.Add(item);
            _changes.Add(new ListChange<T>(ChangeType.Add, 0, item));
        }

        public bool Remove(T item)
        {
            var ret = _items.Remove(item);
            _changes.Add(new ListChange<T>(ChangeType.RemoveItem, 0, item));

            return ret;
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            _changes.Add(new ListChange<T>(ChangeType.RemoveIndex, index, default));
        }

        public void Replace(int index, T item)
        {
            _items[index] = item;
            _changes.Add(new ListChange<T>(ChangeType.Replace, index, item));
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            _changes.Add(new ListChange<T>(ChangeType.Insert, index, item));
        }

        public void Clear()
        {
            _items.Clear();
            _changes.Add(new ListChange<T>(ChangeType.Clear, 0, default));
        }

        public void Sort()
        {
            _items.Sort();
            _changes.Add(new ListChange<T>(ChangeType.Sort, 0, default));
        }

        public void Sort(Comparison<T> comparison)
        {
            _items.Sort(comparison);
            _changes.Add(new ListChange<T>(ChangeType.Sort, 0, default, comparison));
        }

        public bool Contains(T item) => _items.Contains(item);

        public int IndexOf(T item) => _items.IndexOf(item);

        public void FlushChangesToCache()
        {
            var count = _changes.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
                _changes[i].ApplyToList(_cachedItems);

            _changes.Clear();
        }
    }
}
