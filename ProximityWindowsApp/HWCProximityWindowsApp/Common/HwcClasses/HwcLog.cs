using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace HWCProximityWindowsApp.Common.HwcClasses
{
    public static class HwcLog
    {
        private static StorageFolder    rootStorageFolder;
        private static StorageFolder    level1StorageFolder;
        private static StorageFolder    logOutputFolder;

        // Log file handler
        private static StorageFile      fileWriteLog = null;

        // Logging levels to indicate log type
        public enum LoggingLevel
        {
            Verbose = 0,
            Information = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        // Initialize Log class, should be called only once for the running session;
        public static async Task<bool> LogInitializeAsync()
        {
            bool retVale = true;

            if (fileWriteLog == null)
            {
                try
                {
                    var thisPackage = Windows.ApplicationModel.Package.Current;
                    var version = thisPackage.Id.Version;
                    Version appBuildVersion = new Version(version.Major, version.Minor, version.Build, version.Revision);

                    StorageFolder appLocalFolder = ApplicationData.Current.LocalFolder;
                    rootStorageFolder = await appLocalFolder.CreateFolderAsync(HwcConstants.RootDirectoryName, CreationCollisionOption.OpenIfExists);
                    level1StorageFolder = await rootStorageFolder.CreateFolderAsync(HwcConstants.Level1DirectoryName, CreationCollisionOption.OpenIfExists);
                    logOutputFolder = await level1StorageFolder.CreateFolderAsync(HwcConstants.LogDirectoryName, CreationCollisionOption.OpenIfExists);

                    // Create Log file
                    string logFileName = HwcConstants.LogFileName + "_" + GetTimeStamp() + ".log";
                    fileWriteLog = await logOutputFolder.CreateFileAsync(logFileName, CreationCollisionOption.GenerateUniqueName);

                    // Write first log info into the file
                    string firstLogMessage = "Log file created at " + DateTime.Now.ToString() + " " + TimeZoneInfo.Local.StandardName + " (local time)" + Environment.NewLine
                                                + "App package version number [" + appBuildVersion.ToString() + "]" + Environment.NewLine + Environment.NewLine;
                    
                    await FileIO.WriteTextAsync(fileWriteLog, firstLogMessage);
                }
                catch
                {
                    retVale = false;
                }
            }
            else
            {
                LogAsync(LoggingLevel.Warning, "Log file already initialized");
            }

            return (retVale);
        }

        // Log method to be called for logging any info or error
        public static async void LogAsync(HwcLog.LoggingLevel loggingLevel,
                                    string logStr,
                                    [CallerMemberName] string callerFunctionName = "",
                                    [CallerLineNumber] int callerLineNumber = 0
                                    )
        {
            string logString = DateTime.UtcNow.ToString("o") + "::" + " [" + loggingLevel + "] " + logStr + " | "
                + callerFunctionName + "() | " + callerLineNumber + Environment.NewLine;
            try
            {
                await FileIO.AppendTextAsync(fileWriteLog, logString);
            }
            catch
            {
                // Log string not added to the log file;
            }
        }

        private static string GetTimeStamp()
        {
            DateTime now = DateTime.Now;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                 "{0:D2}{1:D2}{2:D2}-{3:D2}{4:D2}{5:D2}{6:D3}",
                                 now.Year,
                                 now.Month,
                                 now.Day,
                                 now.Hour,
                                 now.Minute,
                                 now.Second,
                                 now.Millisecond);
        }
    }
}