﻿namespace Wendlemire.Utils;

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

    public static T RandomElement<T>(this IEnumerable<T> source, Random rng) {
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        ArgumentNullException.ThrowIfNull(rng);

        IList<T> list = source as IList<T> ?? source.ToList();

        if (list.Count == 0) {
            Log.Warning("Getting random element from empty collection.");
            return default!;
        }

        return list[rng.Next(0, list.Count)];
    }

    public static IEnumerable<T> InRandomOrder<T>(this IEnumerable<T> source, Random rng) {
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        ArgumentNullException.ThrowIfNull(rng);

        List<T> elements = source.ToList();

        for (int remainingElements = elements.Count; remainingElements > 0; remainingElements--) {
            int randomIndex = rng.Next(0, remainingElements);
            yield return elements[randomIndex];
            T value = elements[randomIndex];
            elements[randomIndex] = elements[remainingElements - 1];
            elements[remainingElements - 1] = value;
        }
    }

    public static T? RandomElementByWeight<T>(this IEnumerable<T> source, Func<T, float> weightSelector, Random rng) {
        // Materialize to avoid multiple enumeration and cache weights
        var items = source as IList<T> ?? source.ToList();

        if (items.Count == 0) {
            Log.Error("RandomElementByWeight called on empty collection - use TryRandomElementByWeight.");
            return default;
        }

        Span<float> weights = items.Count <= 128 
            ? stackalloc float[items.Count] 
            : new float[items.Count];
        
        float totalWeight = 0f;

        for (int i = 0; i < items.Count; i++) {
            float weight = weightSelector(items[i]);
            if (weight < 0f) {
                Log.Error("Negative weight in selector: " + weight + " from " + items[i]);
                weight = 0f;
            }
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f) {
            Log.Error("RandomElementByWeight with totalWeight=" + totalWeight + " - use TryRandomElementByWeight.");
            return default;
        }

        ArgumentNullException.ThrowIfNull(rng);

        float randomValue = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < items.Count; i++) {
            cumulative += weights[i];
            if (randomValue < cumulative) {
                return items[i];
            }
        }

        // Floating point edge case - return last item
        return items[^1];
    }

    public static V? TryGetValue<T, V>(this IDictionary<T, V?> dict, T key, V? fallback = default) {
        if (!dict.TryGetValue(key, out var value)) {
            return fallback;
        }

        return value;
    }
}