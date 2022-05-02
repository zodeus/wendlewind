using System;
using System.Collections.Generic;
using System.Linq;

namespace Grafted.Utils;

public static class CollectionExtensions {
    public static T? FirstOrNull<T>(this IEnumerable<T?> values, Func<T?, bool> func) where T : class {
        return values.DefaultIfEmpty(null).FirstOrDefault(func);
    }

    public static T? FirstOrNull<T>(this IEnumerable<T?> values) where T : class {
        return values.DefaultIfEmpty(null).FirstOrDefault();
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? matcher = null) {
        return new HashSet<T>(source, matcher);
    }


    public static T? Find<T>(this IList<T> ilist, Predicate<T> match) {
        if (ilist is List<T> list) {
            return list.Find(match);
        }

        if (ilist is T[] array) {
            return Array.Find(array, match);
        }

        return ilist.FirstOrDefault(i => match(i));
    }


    public static List<T> FindAll<T>(this IEnumerable<T> iList, Predicate<T> match) {
        if (iList is List<T> list) {
            return list.FindAll(match);
        }

        if (iList is T[] array) {
            return Array.FindAll(array, match).ToList();
        }

        throw new ArgumentException("Failed to cast IList to either List<T> or T[]");
    }

    public static T RandomElement<T>(this IEnumerable<T> source) {
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        IList<T> list = source as IList<T> ?? source.ToList();

        if (list.Count == 0) {
            Log.Warning("Getting random element from empty collection.");
            return default!;
        }

        return list[Core.Random.Next(0, list.Count)];
    }

    public static IEnumerable<T> InRandomOrder<T>(this IEnumerable<T> source) {
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        List<T> elements = source.ToList();

        for (int remainingElements = elements.Count; remainingElements > 0; remainingElements--) {
            int randomIndex = Core.Random.Next(0, remainingElements);
            yield return elements[randomIndex];
            T value = elements[randomIndex];
            elements[randomIndex] = elements[remainingElements - 1];
            elements[remainingElements - 1] = value;
        }
    }

    public static T? RandomElementByWeight<T>(this IEnumerable<T> source, Func<T, float> weightSelector) {
        float num = 0f;
        IList<T>? list = source as IList<T>;
        if (list != null) {
            for (int i = 0; i < list.Count; i++) {
                float num2 = weightSelector(list[i]);
                if (num2 < 0f) {
                    Log.Error("Negative weight in selector: " + num2 + " from " + list[i]);
                    num2 = 0f;
                }

                num += num2;
            }

            if (list.Count == 1 && num > 0f) {
                return list[0];
            }
        }
        else {
            int num3 = 0;
            foreach (T item in source) {
                num3++;
                float num4 = weightSelector(item);
                if (num4 < 0f) {
                    Log.Error("Negative weight in selector: " + num4 + " from " + item);
                    num4 = 0f;
                }

                num += num4;
            }

            if (num3 == 1 && num > 0f) {
                return source.First();
            }
        }

        if (num <= 0f) {
            Log.Error("RandomElementByWeight with totalWeight=" + num + " - use TryRandomElementByWeight.");
            return default;
        }

        float num5 = (float) Core.Random.NextDouble() * num;
        float num6 = 0f;
        if (list != null) {
            for (int j = 0; j < list.Count; j++) {
                float num7 = weightSelector(list[j]);
                if (!(num7 <= 0f)) {
                    num6 += num7;
                    if (num6 >= num5) {
                        return list[j];
                    }
                }
            }
        }
        else {
            foreach (T item2 in source) {
                float num8 = weightSelector(item2);
                if (!(num8 <= 0f)) {
                    num6 += num8;
                    if (num6 >= num5) {
                        return item2;
                    }
                }
            }
        }

        return default;
    }

    public static V? TryGetValue<T, V>(this IDictionary<T, V?> dict, T key, V? fallback = default) {
        if (!dict.TryGetValue(key, out var value)) {
            return fallback;
        }

        return value;
    }
}