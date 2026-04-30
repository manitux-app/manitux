using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;
using Manitux.Core.Application;

namespace Manitux.Core.Helpers;

public class LogHelper
{
    private static IEventBus _eventBus = CodeLogic.CodeLogic.GetEventBus();

    private static async void CreateLog(LogLevel level, string eventId, string message)
    {
        await _eventBus.PublishAsync(new LogEvent(level, eventId, message));
    }

    public static class Http
    {
        public static void Log(LogLevel level, string message)
            => CreateLog(level, "[HTTP]", message);
    }

    public static class Html
    {
        public static void Log(LogLevel level, string message)
            => CreateLog(level, "[HTML]", message);
    }

    public static class Plugin
    {
        public static void Log(LogLevel level, string pluginId, string message)
            => CreateLog(level, pluginId, message);
    }

    public static class Extractor
    {
        public static void Log(LogLevel level, string extractorId, string message)
            => CreateLog(level, extractorId, message);
    }
}
