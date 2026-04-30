using System;
using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;

namespace Manitux.Core.Application;

public record PluginEvent(string Message) : IEvent;


public record LogEvent(LogLevel Level, string EventId, string Message) : IEvent;

// use CodeLogic Loglevel
// public enum LogLevel
// {
//     None = 0,
//     Info = 1,
//     Warning = 2,
//     Error = 3,
//     Debug = 4
// }