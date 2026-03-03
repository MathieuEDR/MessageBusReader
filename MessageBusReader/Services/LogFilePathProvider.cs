using System;
using System.IO;

namespace MessageBusReader.Services;

internal static class LogFilePathProvider
{
    private static readonly string LogsDirectoryAbsolutePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive - Edrington", "Documents", "Error Queue");
    internal static readonly string LogFilename = Path.Combine(LogsDirectoryAbsolutePath, "Logs.txt");
    internal static readonly string ErrorFilename = Path.Combine(LogsDirectoryAbsolutePath, "Errors.txt");
}
