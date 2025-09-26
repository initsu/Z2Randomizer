using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Z2Randomizer.RandomizerCore;

public interface IWeightedSampler<T>
{
    T Next(Random r);
    IEnumerable<T> Keys();
    IWeightedSampler<T> Remove(T t);
    int Weight(T t);
    IWeightedSampler<T> Clone();
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

    private TableWeightedRandom(T[] table, int totalWeight)
    {
        _table = table;
        _totalWeight = totalWeight;
    }

    public IWeightedSampler<T> Clone()
    {
        return new TableWeightedRandom<T>(_table, _totalWeight);
    }

    public T Next([NotNull] Random r)
    {
        int roll = r.Next(0, _totalWeight);
        return _table[roll];
    }

    public IEnumerable<T> Keys()
    {
        return _table.Distinct();
    }

    public IWeightedSampler<T> Remove(T t)
    {
        var table = _table
            .Where(x => !EqualityComparer<T>.Default.Equals(x, t))
            .ToArray();
        return new TableWeightedRandom<T>(table, table.Length);
    }

    public int Weight(T t)
    {
        return _table.Count(t);
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
            i++;
        }

        _totalWeight = total;
    }

    public LinearWeightedRandom(IEnumerable<KeyValuePair<T, int>> dict)
    : this(dict.Select(kvp => (kvp.Key, kvp.Value)))
    {
    }

    private LinearWeightedRandom(T[] values, int[] cumulativeWeights, int totalWeight)
    {
        _values = values;
        _cumulativeWeights = cumulativeWeights;
        _totalWeight = totalWeight;
    }

    public IWeightedSampler<T> Clone()
    {
        return new LinearWeightedRandom<T>(_values, _cumulativeWeights, _totalWeight);
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

    public IEnumerable<T> Keys()
    {
        return _values;
    }

    public IWeightedSampler<T> Remove(T t)
    {
        int i = 0;
        int length = _values.Length;
        List<T> values = new(length);
        List<int> cumulativeWeights = new(length);
        int lastTotal = 0;
        int removedWeight = 0;

        for (; i < length; i++)
        {
            T v = _values[i]!;
            if (t!.Equals(v))
            {
                removedWeight = _cumulativeWeights[i] - lastTotal;
                break;
            }
            lastTotal = _cumulativeWeights[i];
            values.Add(v);
            cumulativeWeights.Add(lastTotal);
        }
        for (i++; i < length; i++) // append subtracted cumulatives for values that followed the removed value
        {
            values.Add(_values[i]);
            cumulativeWeights.Add(_cumulativeWeights[i] - removedWeight);
        }

        return new LinearWeightedRandom<T>(values.ToArray(), cumulativeWeights.ToArray(), _totalWeight - removedWeight); ;
    }

    public int Weight(T t)
    {
        throw new NotImplementedException();
    }
}
