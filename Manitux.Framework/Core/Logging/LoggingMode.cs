namespace CodeLogic.Core.Logging;

/// <summary>Specifies how log files are organized on disk.</summary>
public enum LoggingMode
{
    /// <summary>
    /// Single rolling file per component. Default.
    /// Rolls when MaxFileSizeMb is reached. Keeps MaxRolledFiles old files.
    /// Files: component.log, component.1.log, component.2.log, ...
    /// </summary>
    SingleFile,

    /// <summary>
    /// Files sorted into year/month/day subdirectories.
    /// Pattern configurable via FileNamePattern.
    /// </summary>
    DateFolder
}
