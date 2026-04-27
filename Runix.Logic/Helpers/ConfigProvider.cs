using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpSqliteORM;
using CSharpSqliteORM.Structure;

namespace GameLibrary.Logic.Helpers;

public class ConfigProvider<ENUMTYPE>
    where ENUMTYPE : struct, Enum
{
    private Dictionary<ENUMTYPE, string?> data;

    private Func<string, string, Task>? handleSave;
    private Func<string, Task>? handleDelete;

    public ConfigProvider(ConfigProvider<ENUMTYPE> copy)
    {
        data = new Dictionary<ENUMTYPE, string?>(copy.data);

        handleDelete = null;
        handleSave = null;
    }

    public ConfigProvider(Dictionary<string, string?> input, Func<string, string, Task> handleSave, Func<string, Task> handleDelete)
    {
        data = new Dictionary<ENUMTYPE, string?>();

        foreach (KeyValuePair<string, string?> pair in input)
        {
            if (!Enum.TryParse(pair.Key, out ENUMTYPE id))
                continue;

            data.Add(id, pair.Value);
        }

        this.handleSave = handleSave;
        this.handleDelete = handleDelete;
    }

    public ConfigProvider(IEnumerable<(string, string?)> input, Func<string, string, Task> handleSave, Func<string, Task> handleDelete)
    {
        data = new Dictionary<ENUMTYPE, string?>();

        foreach ((string key, string? value) in input)
        {
            if (!Enum.TryParse(key, out ENUMTYPE id))
                continue;

            data.Add(id, value);
        }

        this.handleSave = handleSave;
        this.handleDelete = handleDelete;
    }

    // save

    public async Task<bool> SaveGeneric<T>(ENUMTYPE key, T obj)
    {
        if (obj == null)
        {
            if (handleDelete != null)
                await handleDelete(key.ToString());

            return true;
        }

        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        switch (type.Name)
        {
            case nameof(String): return await SaveValue(key, Convert.ToString(obj));
            case nameof(Boolean): return await SaveBool(key, Convert.ToBoolean(obj));

            case nameof(Enum):
            case nameof(Int32): return await SaveInteger(key, Convert.ToInt32(obj));
        }

        throw new Exception($"Invalid type - {typeof(T).Name}");
    }

    public async Task<bool> SaveEnum<T>(ENUMTYPE key, T v) where T : Enum => await SaveInteger(key, Convert.ToInt32(v));
    public async Task<bool> SaveInteger(ENUMTYPE key, int v) => await SaveValue(key, v.ToString());
    public async Task<bool> SaveBool(ENUMTYPE key, bool b) => await SaveValue(key, b ? "1" : "0");
    public async Task<bool> SaveList<T>(ENUMTYPE key, T[] dat) => await SaveValue(key, JsonSerializer.Serialize(dat));

    public async Task<bool> SaveValue(ENUMTYPE key, string? val)
    {
        if (string.IsNullOrEmpty(val))
        {
            if (handleDelete != null)
                await handleDelete(key.ToString());

            data.Remove(key);
            return true;
        }

        if (handleSave != null)
            await handleSave(key.ToString(), val);

        data[key] = val;
        return true;
    }

    // get

    public T GetGeneric<T>(ENUMTYPE key, T defaultVal)
    {
        if (!TryGetValue(key, out string res))
            return defaultVal;

        switch (typeof(T).Name)
        {
            case nameof(Enum): return (T)Enum.ToObject(typeof(T), int.Parse(res));
            case nameof(Boolean): return (T)(object)(res == "1");

            case nameof(String): return (T)(object)(res);
            case nameof(Int32): return (T)(object)int.Parse(res);
        }

        return defaultVal;
    }

    public T GetEnum<T>(ENUMTYPE key, T defaultVal) where T : Enum
    {
        if (TryGetValue(key, out string res))
            return (T)Enum.ToObject(typeof(T), int.Parse(res));

        return defaultVal;
    }

    public bool GetEnum<T>(ENUMTYPE key, out T val) where T : Enum
    {
        if (TryGetValue(key, out string res))
        {
            val = (T)Enum.ToObject(typeof(T), int.Parse(res));
            return true;
        }

        val = default!;
        return false;
    }

    public bool GetInteger(ENUMTYPE key, out int val)
    {
        if (TryGetValue(key, out string res))
        {
            val = int.Parse(res);
            return true;
        }

        val = 0;
        return false;
    }

    public int GetInteger(ENUMTYPE key, int defaultVal)
    {
        if (GetInteger(key, out int v))
            return v;

        return defaultVal;
    }

    public bool GetBoolean(ENUMTYPE key, bool defaultVal)
    {
        if (TryGetValue(key, out string res))
            return res == "1";

        return defaultVal;
    }

    public string? GetValue(ENUMTYPE key)
    {
        if (data.TryGetValue(key, out string? res))
            return res;

        return null;
    }

    public bool TryGetValue(ENUMTYPE key, out string val)
    {
        val = GetValue(key) ?? string.Empty;
        return !string.IsNullOrEmpty(val);
    }

    public bool TryGetList<T>(ENUMTYPE key, out T[] res)
    {
        if (data.TryGetValue(key, out string? raw) && !string.IsNullOrEmpty(raw))
        {
            res = JsonSerializer.Deserialize<T[]>(raw) ?? [];
            return true;
        }

        res = [];
        return false;
    }

    public T[] GetList<T>(ENUMTYPE key)
    {
        _ = TryGetList(key, out T[] res);
        return res;
    }
}
