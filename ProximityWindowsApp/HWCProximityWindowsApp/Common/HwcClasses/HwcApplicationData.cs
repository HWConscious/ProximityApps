using System;
using Windows.Storage;

namespace HWCProximityWindowsApp.Common.HwcClasses
{
    public static class HwcApplicationData
    {
        private static bool IsInitDone = false;
        public static ApplicationDataContainer DataContainer = null;

        // Initialize Application Data, should be called only once for the running session;
        public static void ApplicationDataInitialize()
        {
            if (!IsInitDone)
            {
                DataContainer = Windows.Storage.ApplicationData.Current.LocalSettings;

                // Creating container for 'Configuration' data
                DataContainer.CreateContainer(HwcSettingsCommands.AppConfiguration, Windows.Storage.ApplicationDataCreateDisposition.Always);

                // Setting app specific data
                if (String.IsNullOrEmpty(GetApplicationData(HwcAppConfigurationName.DontAskAgainEnergySavingSetting)))
                {
                    SetApplicationData(HwcAppConfigurationName.DontAskAgainEnergySavingSetting, false.ToString());   // Setting default value in case of null value
                }

                IsInitDone = true;
            }
        }

        // Set Application data; eg, string value = SetApplicationData(ConfigurationName.IP, "127.0.0.1");
        public static void SetApplicationData(string configurationName, string value)
        {
            DataContainer.Values[configurationName] = value;
        }

        // Get back Application data; eg, string value = GetApplicationData(ConfigurationName.IP);
        public static string GetApplicationData(string configurationName)
        {
            string value = null;

            if (DataContainer.Values[configurationName] != null)
            {
                if (!String.IsNullOrEmpty(DataContainer.Values[configurationName].ToString()))
                {
                    value = DataContainer.Values[configurationName].ToString();
                }
                else
                {
                    value = String.Empty;
                }
            }

            return value;
        }
    }
}
