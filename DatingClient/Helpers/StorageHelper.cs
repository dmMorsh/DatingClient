using System.Text.Json;
using DatingClient.Models;

namespace DatingClient.Helpers;

public static class StorageHelper
{
    private const string SearchFilter = "search_filter";

    public static void SaveSearchFilter(SearchFilter filter)
    {
        var json = JsonSerializer.Serialize(filter);
        Preferences.Set(SearchFilter, json);
    }

    public static SearchFilter? LoadSearchFilter()
    { // in any bad case return null, it's ok and hardly reached except empty pref
        try
        {
            var json = Preferences.Get(SearchFilter, "");
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<SearchFilter>(json) ?? null;
        }
        catch
        {
            return null;
        }
    }
}