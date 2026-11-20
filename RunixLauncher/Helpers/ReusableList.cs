using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace RunixLauncher.Helpers;

public class ReusableList<T> : IEnumerable<T> where T : Control
{
    private int currentElementCount;

    private Panel parent;
    private List<T> cachedEntries;
    private Func<T> factory;

    public int getElementCount => currentElementCount;

    public delegate void DrawCallback<DATA>(T element, int index, DATA data);

    public ReusableList(Panel parent)
    {
        this.parent = parent;

        cachedEntries = new List<T>();
        factory = () => Activator.CreateInstance<T>();
    }

    public ReusableList(Panel parent, Func<T> factory)
    {
        this.parent = parent;
        this.factory = factory;

        cachedEntries = new List<T>();
    }

    public async Task DrawAsync<DATA>(Func<Task<DATA[]>> inp, DrawCallback<DATA> draw, int? limit = null)
    {
        DATA[] res;
        currentElementCount = 0;

        try
        {
            res = await inp();
        }
        catch
        {
            throw new Exception("failed to load data for draw");
        }

        Draw(res, draw, limit);
    }

    public void Draw<DATA>(IEnumerable<DATA> inp, DrawCallback<DATA> draw, int? limit = null)
    {
        foreach (var res in DrawInternal(inp, limit))
            draw(res.Item1, res.Item2, res.Item3);
    }

    public async Task DrawWhenAll<DATA>(IEnumerable<DATA> inp, Func<T, int, DATA, Task> draw, int? limit = null)
    {
        await Task.WhenAll(DrawInternal(inp, limit).Select(r => draw(r.Item1, r.Item2, r.Item3)));
    }

    private List<(T, int, DATA)> DrawInternal<DATA>(IEnumerable<DATA> inp, int? limit = null)
    {
        List<(T ui, int pos, DATA dat)> draws = new();
        currentElementCount = inp.Count();

        if (limit.HasValue)
            inp = inp.Take(limit.Value);

        for (int i = 0; i < Math.Max(inp.Count(), cachedEntries.Count); i++)
        {
            if (i >= inp.Count())
            {
                cachedEntries[i].IsVisible = false;
                continue;
            }

            if (i >= cachedEntries.Count)
                CreateEntry();

            cachedEntries[i].IsVisible = true;

            int temp = i;
            draws.Add((cachedEntries[i], temp, inp.ElementAt(i)));
        }

        return draws;
    }

    private void CreateEntry()
    {
        T el = factory();

        cachedEntries.Add(el);
        parent.Children.Add(el);
    }

    public T this[int i]
    {
        get => cachedEntries[i];
    }

    public void Clear()
    {
        DrawInternal((string[])[], 0);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < currentElementCount; i++)
            yield return cachedEntries[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}