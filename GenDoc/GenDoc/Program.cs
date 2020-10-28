using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GenDocAPIs
{
    public class LuaFunctionArgument
    {
        [JsonProperty("alias")] public string Alias { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("optional")] public bool Optional { get; set; }

        [JsonProperty("description")] public string Description { get; set; }
    }

    public class LuaFunctionReturn
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("description")] public string Description { get; set; }
    }

    public class LuaFunction
    {
        [JsonProperty("alias")] public string Alias { get; set; }

        [JsonProperty("arguments")] public List<LuaFunctionArgument> Arguments { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("returns")] public List<LuaFunctionReturn> Returns { get; set; }
    }

    public class LuaFunctionsTable
    {
        [JsonProperty("alias")] public string Alias { get; set; }

        [JsonProperty("functions")] public List<LuaFunction> Functions { get; set; }
    }

    public class Root
    {
        [JsonProperty("MyArray")] public List<LuaFunctionsTable> MyArray { get; set; }
    }

    public static class Helper
    {
        public static string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFileNameWithoutExtension(path));
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            foreach (var s in args)
            {
                var filePath = s;

                var pathIn = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Combine(Directory.GetCurrentDirectory(), filePath));
                var pathOut = Helper.GetFullPathWithoutExtension(pathIn) + ".md";

                if (!File.Exists(pathIn)) return;

                using var streamReader = new StreamReader(pathIn);
                using var streamWriter = new StreamWriter(pathOut, false, Encoding.UTF8);
                var stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("# " + Path.GetFileNameWithoutExtension(filePath) + "\n");

                var json = streamReader.ReadToEndAsync().Result;

                var jsonDeserialized = JsonConvert.DeserializeObject<Root>(json);

                foreach (var luaFunctionsTable in jsonDeserialized.MyArray)
                {
                    stringBuilder.AppendLine($"## {luaFunctionsTable.Alias}\n\n---\n");

                    if (luaFunctionsTable.Functions == null) continue;

                    foreach (var luaFunction in luaFunctionsTable.Functions)
                    {
                        stringBuilder.AppendLine($"### {luaFunction.Alias}\n");

                        stringBuilder.Append($"`{luaFunctionsTable.Alias}.{luaFunction.Alias}(");

                        if (luaFunction.Arguments != null)
                        {
                            var isReadingOptionalArguments = false;
                            var optionalArgumentsToClose = 0;

                            foreach (var luaFunctionArgument in luaFunction.Arguments)
                            {
                                if (luaFunction.Arguments.IndexOf(luaFunctionArgument) == 0)
                                {
                                    stringBuilder.Append($"{luaFunctionArgument.Alias}: {luaFunctionArgument.Type}");
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

                            stringBuilder.Append(')');
                        }

                        if (luaFunction.Returns != null)
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

                        //TODO: Add arguments and returns tables here...

                        stringBuilder.AppendLine($"{luaFunction.Description}\n");
                    }

                }

                streamWriter.Write(stringBuilder);
            }
        }
    }
}