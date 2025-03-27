using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NSdocs.Infrastructure.Data.Converters;

public class EnumToStringConverter<T> : ValueConverter<T, string> where T : Enum
{
    public EnumToStringConverter() : base(
        value => ConvertToString(value),
        value => ConvertFromString(value))
    {
    }

    private static string ConvertToString(T value)
    {
        // Convert PascalCase to kebab-case
        var name = value.ToString();
        return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString().ToLower() : x.ToString().ToLower()));
    }

    private static T ConvertFromString(string value)
    {
        // Convert kebab-case to PascalCase
        var name = string.Concat(value.Split('-').Select(x => char.ToUpper(x[0]) + x.Substring(1)));
        return (T)Enum.Parse(typeof(T), name, true);
    }
}
