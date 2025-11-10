using DatingClient.Models;

namespace DatingClient.Helpers;

public static class GenderValues
{
    public static List<Gender> All { get; } =
        Enum.GetValues(typeof(Gender)).Cast<Gender>().ToList();
}