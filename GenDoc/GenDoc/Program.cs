using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenDoc
{
    internal enum GenDocType
    {
        LuaFunctionsTableArray,
        LuaEventsArray,
        LuaNetpropsDTArray,
    }

    public class Admonition
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }
    public class Example
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }

    public class LuaEventParameter
    {
        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
    public class LuaEvent
    {
        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("parameters")]
        public List<LuaEventParameter> Parameters { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("admonitions")]
        public List<Admonition> Admonitions { get; set; }
    }
    public class LuaEventsTable
    {
        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("events")]
        public List<LuaEvent> Events { get; set; }

        [JsonProperty("examples")]
        public List<Example> Examples { get; set; }
    }

    public class LuaFunctionArgument
    {
        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("optional")]
        public bool Optional { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
    public class LuaFunctionReturn
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
    public class LuaFunction
    {
        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("arguments")]
        public List<LuaFunctionArgument> Arguments { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("returns")]
        public List<LuaFunctionReturn> Returns { get; set; }

        [JsonProperty("admonitions")]
        public List<Admonition> Admonitions { get; set; }
    }
    public class LuaFunctionsTable
    {
        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("functions")]
        public List<LuaFunction> Functions { get; set; }

        [JsonProperty("examples")]
        public List<Example> Examples { get; set; }
    }

    public class Root
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("MyArray")]
        public JArray MyArray { get; set; }
    }

    public static class Helper
    {
        public static string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFileNameWithoutExtension(path));
        }

        public static List<string> GetFilesDeep(string root, string regex, int depth)
        {
            var files = new List<string>();

            foreach (var directory in Directory.EnumerateDirectories(root))
            {
                if (depth > 0)
                    files.AddRange(GetFilesDeep(directory, regex, depth - 1));
            }

            var filesFound = Directory.EnumerateFiles(root);

            foreach (var fileFound in filesFound)
            {
                if (Regex.Match(fileFound, regex).Success)
                    files.Add(fileFound);
            }

            return files;
        }
    }

    internal static class GenDoc
    {
        private static readonly Dictionary<GenDocType, Action<StringBuilder, Root>> TypeToAction =
            new Dictionary<GenDocType, Action<StringBuilder, Root>>
            {
                {
                    GenDocType.LuaFunctionsTableArray,
                    (stringBuilder, jsonMapRoot) =>
                    {
                        var jsonMapRootCastMyArray = jsonMapRoot.MyArray.ToObject<List<LuaFunctionsTable>>();

                        if (jsonMapRootCastMyArray == null)
                            return;

                        foreach (var luaFunctionsTable in jsonMapRootCastMyArray)
                        {
                            stringBuilder.AppendLine($"## {luaFunctionsTable.Alias}\n\n---\n");

                            if (luaFunctionsTable.Functions == null) continue;

                            /*
                             * Write functions data
                             */
                            foreach (var luaFunction in luaFunctionsTable.Functions)
                            {
                                stringBuilder.AppendLine($"### {luaFunction.Alias}\n");

                                if (!string.IsNullOrEmpty(luaFunction.Description))
                                    stringBuilder.AppendLine($"{luaFunction.Description}\n");

                                stringBuilder.Append(
                                    $"`{luaFunctionsTable.Alias}{(luaFunctionsTable.Alias.Contains("{}") ? ":" : ".")}{luaFunction.Alias}(");

                                var argumentsExists = luaFunction.Arguments != null;
                                var returnsExists = luaFunction.Returns != null;

                                /*
                                 * Write arguments
                                 */
                                if (argumentsExists)
                                {
                                    var isReadingOptionalArguments = false;
                                    var optionalArgumentsToClose = 0;

                                    foreach (var luaFunctionArgument in luaFunction.Arguments)
                                    {
                                        if (luaFunction.Arguments.IndexOf(luaFunctionArgument) == 0)
                                        {
                                            stringBuilder.Append(
                                                $"{luaFunctionArgument.Alias}: {luaFunctionArgument.Type}");
                                        }
                                        else
                                        {
                                            if (luaFunctionArgument.Optional)
                                            {
                                                isReadingOptionalArguments = true;
                                                optionalArgumentsToClose++;
                                            }

                                            stringBuilder.Append(isReadingOptionalArguments
                                                ? $" [, {luaFunctionArgument.Alias}: {luaFunctionArgument.Type}"
                                                : $", {luaFunctionArgument.Alias}: {luaFunctionArgument.Type}");
                                        }
                                    }

                                    for (var i = 0; i < optionalArgumentsToClose; i++)
                                    {
                                        stringBuilder.Append(']');
                                    }
                                }

                                stringBuilder.Append(')');

                                /*
                                 * Write returns
                                 */
                                if (returnsExists)
                                {
                                    if (luaFunction.Returns?.Count > 0)
                                        stringBuilder.Append(" : ");


                                    foreach (var luaFunctionReturn in luaFunction.Returns)
                                    {
                                        stringBuilder.Append(luaFunction.Returns.IndexOf(luaFunctionReturn) == 0
                                            ? $"{luaFunctionReturn.Type}"
                                            : $", {luaFunctionReturn.Type}");
                                    }
                                }

                                stringBuilder.AppendLine("`\n");

                                /*
                                 * Write table of arguments
                                 */
                                if (argumentsExists && luaFunction.Arguments.Count > 0)
                                {
                                    stringBuilder.AppendLine("|Argument|Type|Optional|Description|");
                                    stringBuilder.AppendLine("|-|-|-|-|");

                                    foreach (var luaFunctionArgument in luaFunction.Arguments)
                                    {
                                        stringBuilder.AppendLine(
                                            $"|{luaFunctionArgument.Alias}|{luaFunctionArgument.Type}|{(luaFunctionArgument.Optional ? "Yes" : "No")}|{luaFunctionArgument.Description}|");
                                    }

                                    stringBuilder.Append("\n");
                                }

                                /*
                                 * Write table of returns
                                 */
                                if (returnsExists && luaFunction.Returns.Count > 0)
                                {
                                    stringBuilder.AppendLine("|Return|Type|Description|");
                                    stringBuilder.AppendLine("|-|-|-|");

                                    foreach (var (value, i) in luaFunction.Returns.Select((value, i) => (value, i)))
                                    {
                                        var positionalIndex = (i + 1) switch
                                        {
                                            1 => "1st",
                                            2 => "2nd",
                                            3 => "3rd",
                                            _ => $"{i + 1}th"
                                        };

                                        stringBuilder.AppendLine(
                                            $"|{positionalIndex}|{value.Type}|{value.Description}|");
                                    }

                                    stringBuilder.Append("\n");
                                }

                                /*
                                 * Write admonitions
                                 */
                                if (luaFunction.Admonitions != null && luaFunction.Admonitions.Count > 0)
                                {
                                    foreach (var luaFunctionAdmonition in luaFunction.Admonitions)
                                    {
                                        stringBuilder.AppendLine($"!!! {luaFunctionAdmonition.Type} \"{luaFunctionAdmonition.Title}\"\n");

                                        foreach (var admonitionDataLine in luaFunctionAdmonition.Data.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.None))
                                        {
                                            stringBuilder.AppendLine($"    {admonitionDataLine}");
                                        }

                                        stringBuilder.Append("\n");
                                    }
                                }
                            }

                            /*
                             * Write examples data
                             */
                            if (luaFunctionsTable.Examples != null && luaFunctionsTable.Examples.Count > 0)
                            {
                                stringBuilder.AppendLine("## Examples\n");

                                stringBuilder.AppendLine("??? example \"Examples\"");

                                foreach (var example in luaFunctionsTable.Examples)
                                {
                                    stringBuilder.AppendLine($"\n    === \"{example.Title}\"\n");

                                    stringBuilder.AppendLine($"        ``` {example.Language} linenums=\"1\"");

                                    foreach (var exampleDataLine in example.Data.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.None))
                                    {
                                        stringBuilder.AppendLine($"        {exampleDataLine}");
                                    }

                                    stringBuilder.AppendLine("        ```");
                                }
                            }
                        }
                    }
                },
                {
                    GenDocType.LuaEventsArray,
                    (stringBuilder, jsonMapRoot) =>
                    {
                        var jsonMapRootCastMyArray = jsonMapRoot.MyArray.ToObject<List<LuaEventsTable>>();

                        if (jsonMapRootCastMyArray == null)
                            return;

                        foreach (var luaEventsTable in jsonMapRootCastMyArray)
                        {
                            stringBuilder.AppendLine($"## {luaEventsTable.Alias}\n\n---\n");

                            if (luaEventsTable.Events == null) continue;

                            /*
                             * Write events data
                             */
                            foreach (var luaEvent in luaEventsTable.Events)
                            {
                                stringBuilder.AppendLine($"### {luaEvent.Alias}\n");

                                if (!string.IsNullOrEmpty(luaEvent.Description))
                                    stringBuilder.AppendLine($"{luaEvent.Description}\n");

                                /*
                                 * Write table of parameters
                                 */
                                if (luaEvent.Parameters != null && luaEvent.Parameters.Count > 0)
                                {
                                    stringBuilder.AppendLine("|Parameter|Type|Description|");
                                    stringBuilder.AppendLine("|-|-|-|");

                                    foreach (var luaEventParameter in luaEvent.Parameters)
                                    {
                                        stringBuilder.AppendLine(
                                            $"|e{luaEventParameter.Alias}|{luaEventParameter.Type}|{luaEventParameter.Description}|");
                                    }

                                    stringBuilder.Append("\n");
                                }

                                /*
                                 * Write admonitions
                                 */
                                if (luaEvent.Admonitions != null && luaEvent.Admonitions.Count > 0)
                                {
                                    foreach (var luaFunctionAdmonition in luaEvent.Admonitions)
                                    {
                                        stringBuilder.AppendLine($"!!! {luaFunctionAdmonition.Type} \"{luaFunctionAdmonition.Title}\"\n");

                                        foreach (var admonitionDataLine in luaFunctionAdmonition.Data.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.None))
                                        {
                                            stringBuilder.AppendLine($"    {admonitionDataLine}");
                                        }

                                        stringBuilder.Append("\n");
                                    }
                                }
                            }

                            /*
                             * Write examples data
                             */
                            if (luaEventsTable.Examples != null && luaEventsTable.Examples.Count > 0)
                            {
                                stringBuilder.AppendLine("## Examples\n");

                                stringBuilder.AppendLine("??? example \"Examples\"");

                                foreach (var example in luaEventsTable.Examples)
                                {
                                    stringBuilder.AppendLine($"\n    === \"{example.Title}\"\n");

                                    stringBuilder.AppendLine($"        ``` {example.Language} linenums=\"1\"");

                                    foreach (var exampleDataLine in example.Data.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.None))
                                    {
                                        stringBuilder.AppendLine($"        {exampleDataLine}");
                                    }

                                    stringBuilder.AppendLine("        ```");
                                }
                            }
                        }
                    }
                },
            };

        private static void ForType(GenDocType type, StringBuilder stringBuilder, Root jsonMapRoot)
        {
            if (TypeToAction.ContainsKey(type))
                TypeToAction[type](stringBuilder, jsonMapRoot);
        }

        public static void ForFile(string path)
        {
            var pathIn = Path.GetRelativePath(Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), path));
            var pathOut = Helper.GetFullPathWithoutExtension(pathIn) + ".md";

            if (!File.Exists(pathIn)) return;

            using var streamReader = new StreamReader(pathIn);
            using var streamWriter = new StreamWriter(pathOut, false, Encoding.UTF8);
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("# " + Path.GetFileNameWithoutExtension(path) + "\n");

            var jsonMapRoot = JsonConvert.DeserializeObject<Root>(streamReader.ReadToEndAsync().Result);

            if (jsonMapRoot == null) return;

            Enum.TryParse(typeof(GenDocType), jsonMapRoot.Type, out var docType);

            if (docType == null) return;

            ForType((GenDocType)docType, stringBuilder, jsonMapRoot);

            streamWriter.Write(stringBuilder);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            foreach (var filepath in Helper.GetFilesDeep(Path.Combine(Directory.GetCurrentDirectory(), args[0]), ".*\\.json$", 10))
            {
                GenDoc.ForFile(filepath);
            }
        }
    }
}