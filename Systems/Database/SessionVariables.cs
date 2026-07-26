using System.Collections.Generic;
using LabApi.Features.Wrappers;

public static class SessionVariables
{
    private static readonly Dictionary<Player, Dictionary<string, object>> _data = new();

    public static Dictionary<string, object> Get(Player player)
    {
        if (!_data.TryGetValue(player, out var dict))
        {
            dict = new Dictionary<string, object>();
            _data[player] = dict;
        }
        return dict;
    }

    public static void Set(Player player, string key, object value)
    {
        Get(player)[key] = value;
    }

    public static bool TryGet<T>(Player player, string key, out T value)
    {
        if (Get(player).TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }

    public static void Clear(Player player)
    {
        _data.Remove(player);
    }
}