#!/usr/bin/env dotnet
#:project ../../src/AslHelp.Shared/AslHelp.Shared.csproj
#:package System.CommandLine
#:property PublishAot=false
#:include Settings.cs

using AslHelp.Options.Binding;
using AslHelp.Options.Schema;
using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

Argument<FileInfo> input = new("input")
{
    Description = "The settings document to read.",
    DefaultValueFactory = _ => new("inputs/settings.jsonc"),
};

Option<bool> schema = new("--schema")
{
    Description = "Print the JSON schema of the settings instead of reading a document.",
};

RootCommand root = [with("Reads a JSONC settings document, checks it and prints what it bound."), input, schema];

root.SetAction(result => result.GetValue(schema) ? printSchema() : print(result.GetValue(input)!.FullName));

return root.Parse(args).Invoke();

static int printSchema()
{
    Console.WriteLine(OptionsSchemaExporter.Export(typeof(Settings), "Sample settings", colonKeys: true));

    return 0;
}

static int print(string path)
{
    try
    {
        var settings = load(path);

        Console.WriteLine($"level:  {settings.Level}");
        Console.WriteLine($"size:   {settings.Size} (0x{settings.Size:X})");
        Console.WriteLine($"output: {settings.Output.Kind}, {settings.Output.Path ?? "no path"}");

        return 0;
    }
    catch (JsonException e)
    {
        Console.Error.WriteLine($"{Path.GetFileName(path)}: {e.Message}");

        return 1;
    }
}

// Reads the document, expands its colon keys, binds it and checks the bound values.
static Settings load(string path)
{
    var node = JsonNode.Parse(
        File.ReadAllText(path),
        documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

    ColonKeys.Expand(node);

    var root = JsonSerializer.SerializeToElement(node);
    var settings = OptionsBinder.Bind<Settings>(root, "$schema") ?? throw new JsonException("The document is null.");

    string name = Path.GetFileName(path);

    OptionsValidator.CheckValues(settings, name);
    OptionsValidator.Require(settings, name);

    return settings;
}
