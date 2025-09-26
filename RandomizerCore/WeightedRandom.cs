using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Z2Randomizer.RandomizerCore;

public interface IWeightedSampler<T>
{
    T Next(Random r);
}


/// Best option for small weight sums
/// Construction time proportional to total weight sum.
/// Memory usage proportional to total weight sum (bad if weights are large).
/// Sampling is O(1).
public class TableWeightedRandom<T> : IWeightedSampler<T>
{
    private readonly T[] _table;
    private readonly int _totalWeight;

    public TableWeightedRandom(IEnumerable<(T, int)> entries)
    {
        int size;
        if (entries == null || (size = entries.Count()) == 0) { throw new ArgumentException("Entries cannot be null or empty.", nameof(entries)); }

        List<T> table = new();
        foreach (var (value, weight) in entries)
        {
            for (int i = 0; i < weight; i++)
            {
                table.Add(value);
            }
        }
        _table = table.ToArray();
        _totalWeight = _table.Length;
    }

    public TableWeightedRandom(IEnumerable<KeyValuePair<T, int>> dict)
    : this(dict.Select(kvp => (kvp.Key, kvp.Value)))
    {
    }

    public T Next([NotNull] Random r)
    {
        int roll = r.Next(0, _totalWeight);
        return _table[roll];
    }
}

/// Good for modest n (hundreds or less) where total weight is too big for TableWeightedRandom
/// Construction time O(n).
/// Memory usage O(n), independent of weight magnitudes.
/// Sampling is O(n) (linear scan).

public class LinearWeightedRandom<T> : IWeightedSampler<T>
{
    private readonly T[] _values;
    private readonly int[] _cumulativeWeights;
    private readonly int _totalWeight;

    public LinearWeightedRandom(IEnumerable<(T, int)> entries)
    {
        int size;
        if (entries == null || (size = entries.Count()) == 0) { throw new ArgumentException("Entries cannot be null or empty.", nameof(entries)); }

        _values = new T[size];
        _cumulativeWeights = new int[size];

        int i = 0;
        int total = 0;
        foreach (var (value, weight) in entries)
        {
            if (weight < 0) { throw new ArgumentException($"Weight must be positive (entry {i})."); }

            total += weight;
            _values[i] = value;
            _cumulativeWeights[i] = total;
        }

        _totalWeight = total;
    }

    public LinearWeightedRandom(IEnumerable<KeyValuePair<T, int>> dict)
    : this(dict.Select(kvp => (kvp.Key, kvp.Value)))
    {
    }

    public T Next([NotNull] Random r)
    {
        int roll = r.Next(0, _totalWeight);

        for (int i = 0; i < _cumulativeWeights.Length; ++i)
        {
            if (roll < _cumulativeWeights[i])
            {
                return _values[i];
            }
        }

        throw new ImpossibleException("Failed to select a value.");
    }
}
