namespace HWC_ProximityWindowsApp.ProximityApp.Models
{
    #region General constants

    /// <summary>
    /// General constants
    /// </summary>
    public static class Constants
    {
        // Directory and file related constants
        public const string RootDirectoryName = "HwConscious";
        public const string Level1DirectoryName = "ProximityWindowsApp";
        public const string LogDirectoryName = "Logs";
        public const string LogFileName = "Log";

        // REST API related constants
        public const string RestApiEndpoint = "https://oz3yqvjaik.execute-api.us-east-1.amazonaws.com/v1";
        public const string XApiKeyValue = "kHnzbQx6PX6sLZIIwwP2E58QlLKKUHeAao4fzoX0";

        // Miscellaneous constants
        public const int NotificationPullFrequencyInMs = 2000;
        public const int UserEventConfirmationDurationInMs = 5000;
    }

    #endregion

    #region Other constants

    /// <summary>
    /// Settings commands
    /// </summary>
    public struct SettingsCommands
    {
        public const string AppConfiguration = "AppConfiguration";
        public const string About = "About";
    }

    /// <summary>
    /// Configuration names
    /// </summary>
    public struct ConfigurationNames
    {
        public const string DontAskAgainEnergySavingSetting = "DontAskAgainEnergySavingSetting";
        public const string DisplayEndpointID = "DisplayEndpointID";
    }
    
    #endregion
}
