namespace HWCProximityWindowsApp.Common.HwcClasses
{
    // Constants
    public static class HwcConstants
    {
        // Directory and file related constants
        public const string RootDirectoryName = "HwConscious";
        public const string Level1DirectoryName = "ProximityWindowsApp";
        public const string LogDirectoryName = "Logs";
        public const string LogFileName = "Log";
    }

    // Settings Commands
    public struct HwcSettingsCommands
    {
        public const string AppConfiguration = "AppConfiguration";
        public const string About = "About";
    }

    // Configuration Names
    public struct HwcAppConfigurationName
    {
        public const string DontAskAgainEnergySavingSetting = "DontAskAgainEnergySavingSetting";
    }
}
