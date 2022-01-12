﻿using System;
using System.Collections.Concurrent;

namespace EmitMapper.Utils;

public struct LazyConcurrentDictionary<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _inner;

    public LazyConcurrentDictionary()
    {
        _inner = new ConcurrentDictionary<TKey, Lazy<TValue>>(Environment.ProcessorCount, 1024);
    }

    public LazyConcurrentDictionary(int concurrencyLevel, int capacity)
    {
        _inner = new ConcurrentDictionary<TKey, Lazy<TValue>>(concurrencyLevel, capacity);
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        return _inner.GetOrAdd(
            key,
            k => new Lazy<TValue>(() => valueFactory(k))
        ).Value;
    }

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory,
        Func<TKey, TValue, TValue> updateValueFactory)
    {
        return _inner.AddOrUpdate(
            key,
            k => new Lazy<TValue>(() => addValueFactory(k)),
            (k, currentValue) => new Lazy<TValue>(() => updateValueFactory(k, currentValue.Value))
        ).Value;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        value = default;

        var result = _inner.TryGetValue(key, out var v);

        if (result) value = v.Value;

        return result;
    }

    //  overload may not make sense to use when you want to avoid
    //  the construction of the value when it isn't needed
    public bool TryAdd(TKey key, TValue value)
    {
        return _inner.TryAdd(key, new Lazy<TValue>(() => value));
    }

    public bool TryAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        return _inner.TryAdd(
            key,
            new Lazy<TValue>(() => valueFactory(key))
        );
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        value = default;

        if (_inner.TryRemove(key, out var v))
        {
            value = v.Value;
            return true;
        }

        return false;
    }

    public bool TryUpdate(TKey key, Func<TKey, TValue, TValue> updateValueFactory)
    {
        if (!_inner.TryGetValue(key, out var existingValue))
            return false;

        return _inner.TryUpdate(
            key,
            new Lazy<TValue>(
                () => updateValueFactory(key, existingValue.Value)
            ),
            existingValue
        );
    }
}