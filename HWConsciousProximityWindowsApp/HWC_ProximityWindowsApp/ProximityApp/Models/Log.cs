using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace HWC_ProximityWindowsApp.ProximityApp.Models
{
    /// <summary>
    /// Log manager, stores logs of each app-running-session into separate log files at app's local folder.
    /// </summary>
    public static class Log
    {
        #region Data members

        private static StorageFolder _rootStorageFolder = null;
        private static StorageFolder _level1StorageFolder = null;
        private static StorageFolder _logOutputFolder = null;
        private static StorageFile _fileWriteLog = null;    // Log file handler
        private static SemaphoreSlim _fileLock = new SemaphoreSlim(initialCount: 1);

        /// <summary>
        /// Logging levels to indicate log type.
        /// </summary>
        public enum LoggingLevel
        {
            Verbose = 0,
            Information = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        #endregion

        #region Initialize

        /// <summary>
        /// Initializes Log model, should be called only once for the app-running-session.
        /// </summary>
        /// <returns>Initialization status</returns>
        public static async Task LogInitializeAsync()
        {
            if (_fileWriteLog == null)
            {
                try
                {
                    var thisPackage = Windows.ApplicationModel.Package.Current;
                    var version = thisPackage.Id.Version;
                    Version appBuildVersion = new Version(version.Major, version.Minor, version.Build, version.Revision);

                    StorageFolder appLocalFolder = ApplicationData.Current.LocalFolder;
                    _rootStorageFolder = await appLocalFolder.CreateFolderAsync(Constants.RootDirectoryName, CreationCollisionOption.OpenIfExists);
                    _level1StorageFolder = await _rootStorageFolder.CreateFolderAsync(Constants.Level1DirectoryName, CreationCollisionOption.OpenIfExists);
                    _logOutputFolder = await _level1StorageFolder.CreateFolderAsync(Constants.LogDirectoryName, CreationCollisionOption.OpenIfExists);

                    // Create Log file
                    string logFileName = Constants.LogFileName + "_" + GetTimeStamp() + ".log";
                    _fileWriteLog = await _logOutputFolder.CreateFileAsync(logFileName, CreationCollisionOption.GenerateUniqueName);

                    // Write first log info into the file
                    string firstLogMessage = "Log file created at " + DateTime.Now.ToString() + " " + TimeZoneInfo.Local.StandardName + " (local time)" + Environment.NewLine
                                                + "App package version number [" + appBuildVersion.ToString() + "]" + Environment.NewLine + Environment.NewLine;
                    await FileIO.WriteTextAsync(_fileWriteLog, firstLogMessage);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error in Log initialization. " + ex.Message);
                }
            }
            else
            {
                LogAsync(LoggingLevel.Warning, "Log file already initialized");
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Writes log string into log file.
        /// </summary>
        /// <param name="loggingLevel">Logging levels to indicate log type</param>
        /// <param name="logString">Log string</param>
        /// <param name="callerFunctionName">Name of source function</param>
        /// <param name="callerLineNumber">Line number of source code</param>
        public static async void LogAsync(Log.LoggingLevel loggingLevel, string logString, [CallerMemberName] string callerFunctionName = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            string log = (loggingLevel == LoggingLevel.Information) ?
                DateTime.UtcNow.ToString("o") + "::" + " [" + loggingLevel + "] " + logString + Environment.NewLine :
                DateTime.UtcNow.ToString("o") + "::" + " [" + loggingLevel + "] " + logString + " | " + callerFunctionName + "() | " + callerLineNumber + Environment.NewLine;

            await _fileLock.WaitAsync();
            try
            {
                await FileIO.AppendTextAsync(_fileWriteLog, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in adding log string to the log file. " + ex.Message);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Gets full path of the log folder in the file system.
        /// </summary>
        /// <returns>Full path of the log folder</returns>
        public static string GetLogFolderPath()
        {
            string logFolderPath = null;
            try
            {
                logFolderPath = _logOutputFolder.Path;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in getting log folder path. " + ex.Message);
            }
            return logFolderPath;
        }

        /// <summary>
        /// Gets name of the log file for the app-running-session including the file name extension.
        /// </summary>
        /// <returns>Name of the log file</returns>
        public static string GetLogFileName()
        {
            string logFileName = null;
            try
            {
                logFileName = _fileWriteLog.Name;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in getting log file name. " + ex.Message);
            }
            return logFileName;
        }

        /// <summary>
        /// Writes log string in the output window when debugging.
        /// </summary>
        /// <param name="logString">Log string</param>
        public static void DebugLog(string logString)
        {
            if (!string.IsNullOrEmpty(logString))
            {
                Debug.WriteLine("[" + DateTime.Now.ToString() + "] " + logString);
            }
        }

        #endregion

        #region Private methods

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

        #endregion
    }
}
