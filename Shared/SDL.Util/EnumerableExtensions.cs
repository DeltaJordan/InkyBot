using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SDL.Util.Enums;

namespace SDL.Util
{

    public delegate bool TryGetFunc<TIn, TOut>(TIn input, out TOut output);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class EnumerableExtensions
    {

        public static IEnumerable<TOut> SelectWhere<TIn, TOut>(
            this IEnumerable<TIn> list,
            TryGetFunc<TIn, TOut> selector
        )
        {
            foreach (TIn element in list)
            {
                if (selector(element, out TOut value))
                {
                    yield return value;
                }
            }
        }

        public static ICollection<T> CoerceToICollection<T>(this IEnumerable<T> list)
        {
            return list as ICollection<T> ?? new List<T>(list);
        }

        public static IReadOnlyList<T> CoerceToIReadOnlyList<T>(this IEnumerable<T> list)
        {
            return list as IReadOnlyList<T> ?? new List<T>(list);
        }

        public static IList<T> CoerceToIList<T>(this IEnumerable<T> list)
        {
            return list as IList<T> ?? new List<T>(list);
        }

        public static List<T> CoerceToList<T>(this IEnumerable<T> list)
        {
            return list as List<T> ?? new List<T>(list);
        }

        public static IEnumerable<T> Stored<T>(this IEnumerable<T> list)
        {
            if (list is ICollection<T> || list is IReadOnlyCollection<T>) return list;
            return list.ToList();
        }

        public static SortedSet<T> ToSortedSet<T>(this IEnumerable<T> list)
        {
            SortedSet<T> set = new();
            foreach (T item in list)
            {
                if (set.Contains(item)) continue;
                set.Add(item);
            }
            return set;
        }

        /// <summary>
        /// Transforms the list in-place and returns a reference to the list.
        /// </summary>
        public static IList<T> Transform<T>(
            this IList<T> list,
            Func<T, T> transform
        )
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = transform(list[i]);
            }
            return list;
        }

        /// <summary>
        /// Attempts to transform the list in-place. If the implementation is
        /// not a list or array, a copy is made instead. The returned reference
        /// may or may not be a reference to the original list.
        ///
        /// As this bypasses the read-only nature of the list, and the return
        /// value cannot be relied upon to be either the same or different from
        /// the intial list reference, this should only be used when the original
        /// state of the list will no longer be used.
        /// </summary>
        public static IReadOnlyList<T> SelectOrTransform<T>(
            this IReadOnlyList<T> list,
            Func<T, T> transform
        )
        {
            if (list is IList<T> mutableList)
            {
                mutableList.Transform(transform);
                return list;
            }
            else
            {
                return list.Select(transform).ToList();
            }
        }

        public static IEnumerable<V> Resolve<K, V>(
            this IEnumerable<K> keys,
            IDictionary<K, V> lookup
        )
        {
            foreach (K key in keys)
            {
                if (lookup.TryGetValue(key, out V value))
                {
                    yield return value;
                }
            }
        }

        public static IEnumerable<V> Resolve<K, V>(
            this IEnumerable<K> keys,
            IReadOnlyDictionary<K, V> lookup
        )
        {
            foreach (K key in keys)
            {
                if (lookup.TryGetValue(key, out V value))
                {
                    yield return value;
                }
            }
        }

        public static IEnumerable<T> SortBy<T, K>(
            this IEnumerable<T> list,
            Func<T, K> keySelector,
            SortOrder sortOrder
        )
        {
            return sortOrder switch
            {
                SortOrder.Ascending => list.OrderBy(keySelector),
                SortOrder.Descending => list.OrderByDescending(keySelector),
                _ => throw new InvalidEnumArgumentException(nameof(sortOrder), (int)sortOrder, typeof(SortOrder))
            };
        }

    }

}
