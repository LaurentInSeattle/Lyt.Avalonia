namespace Lyt.Avalonia.Localizer;

using System;
using System.Collections.Generic;
using System.Text;

public static class AxamlParserWriter
{
    private const string ResourceDictionaryHeader =
@"
<ResourceDictionary 
    xmlns=""https://github.com/avaloniaui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:system=""clr-namespace:System;assembly=System.Runtime""
    >
";

    private const string ResourceDictionaryFooter =
@"
</ResourceDictionary>
";

    private const string ResourceDictionaryEntryFormat =
@"
    <system:String x:Key=""{0}"">{1}</system:String>
";

    private static readonly List<string> ResourceDictionaryEntryTokens =
        [
            "<system:String x:Key=\"",
            "\">",
            "</system:String>"
        ];

    private static readonly int ResourceDictionaryMinimumLength =
        ResourceDictionaryHeader.Length +
        ResourceDictionaryFooter.Length +
        ResourceDictionaryEntryFormat.Length;

    public static Tuple<bool, Dictionary<string, string>> ParseResourceFile(string fileContent)
    {
        try
        {
            // Split handling both Windows and Linux carriage returns safely
            string[] lines =
                fileContent.Split(
                    ["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            Dictionary<string, string> dictionary = [];
            string lineStartsWith = ResourceDictionaryEntryTokens[0];
            string lineSpliter = ResourceDictionaryEntryTokens[1];
            string lineEndsWith = ResourceDictionaryEntryTokens[2];
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if ((trimmedLine.Length == 0) ||
                    (!trimmedLine.StartsWith(lineStartsWith)) ||
                    (!trimmedLine.EndsWith(lineEndsWith)))
                {
                    continue;
                }

                trimmedLine = trimmedLine.Replace(lineStartsWith, string.Empty);
                trimmedLine = trimmedLine.Replace(lineEndsWith, string.Empty);
                string[] tokens =
                    trimmedLine.Split(lineSpliter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length != 2)
                {
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                dictionary.Add(key, value);
            }

            return Tuple.Create(true, dictionary);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
            return Tuple.Create(false, new Dictionary<string, string>());
        }
    }

    public static bool CreateResourceFile(string destinationPath, Dictionary<string, string> dictionary)
    {
        try
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(ResourceDictionaryHeader);
            foreach (var item in dictionary)
            {
                string key = item.Key;
                string value = item.Value;
                string line = string.Format(ResourceDictionaryEntryFormat, key, value);
                stringBuilder.Append(line);
            }

            stringBuilder.Append(ResourceDictionaryFooter);
            File.WriteAllText(destinationPath, stringBuilder.ToString());

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
            return false;
        }
    }
}
