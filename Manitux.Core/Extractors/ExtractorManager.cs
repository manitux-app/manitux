using System;
using System.Reflection;

namespace Manitux.Core.Extractors;

public static class ExtractorManager
{
    private static readonly IEnumerable<ExtractorBase>? _services;

    static ExtractorManager()
    {
        _services = Assembly.GetExecutingAssembly()
               .GetTypes()
               .Where(t => typeof(ExtractorBase).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
               .Select(t => Activator.CreateInstance(t) as ExtractorBase)
               .Where(s => s != null)
               .Cast<ExtractorBase>()
               .ToList();

        // foreach (var s in _services)
        // {
        //     System.Console.WriteLine(s.Name);
        // }

    }

    public static string Initialize()
    {
        return "Initialize";
    }

    public static ExtractorBase? GetExtractorByUrl(string url)
    {
        var service = _services?.FirstOrDefault(s => s.CanHandleUrl(url));

        if (service is null) return null;

        return service;
    }

    public static ExtractorBase? GetExtractorByName(string name)
    {
        var service = _services?.FirstOrDefault(s => s.Name == name);

        if (service is null) return null;

        return service;
    }
}
