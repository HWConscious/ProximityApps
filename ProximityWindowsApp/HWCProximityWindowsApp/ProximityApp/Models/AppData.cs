using System;
using Windows.Storage;

namespace HWCProximityWindowsApp.ProximityApp.Models
{
    /// <summary>
    /// Local data manager
    /// </summary>
    public static class AppData
    {
        #region Data members

        private static bool _isInitDone = false;
        private static ApplicationDataContainer _dataContainer = null;

        #endregion

        #region Initialize

        /// <summary>
        /// Initializes AppData model, should be called only once for the app-running-session.
        /// </summary>
        public static void AppDataInitialize()
        {
            if (!_isInitDone)
            {
                try
                { 
                    _dataContainer = ApplicationData.Current.LocalSettings;

                    // Creating container for 'Configuration' data
                    _dataContainer.CreateContainer(SettingsCommands.AppConfiguration, ApplicationDataCreateDisposition.Always);

                    // Setting app specific data
                    if (String.IsNullOrEmpty(GetAppData(ConfigurationNames.DontAskAgainEnergySavingSetting)))
                    {
                        SetAppData(ConfigurationNames.DontAskAgainEnergySavingSetting, false.ToString());   // Configuring default value in case of null value
                    }
                    if (String.IsNullOrEmpty(GetAppData(ConfigurationNames.DisplayEndpointID)))
                    {
                        SetAppData(ConfigurationNames.DisplayEndpointID, string.Empty);    // Configuring default value in case of null value
                    }

                    _isInitDone = true;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error in AppData initialization. " + ex.Message);
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Sets an app data.
        /// </summary>
        /// <param name="configurationName">E.g., ConfigurationNames.DisplayEndpointID</param>
        /// <param name="value">E.g., "100"</param>
        public static void SetAppData(string configurationName, string value)
        {
            try
            {
                _dataContainer.Values[configurationName] = value;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in settting app data. " + ex.Message);
            }
        }
        
        /// <summary>
        /// Gets an app data.
        /// </summary>
        /// <param name="configurationName">E.g., ConfigurationNames.DisplayEndpointID</param>
        /// <returns></returns>
        public static string GetAppData(string configurationName)
        {
            string value = null;

            try
            {
                if (_dataContainer.Values[configurationName] != null)
                {
                    if (!String.IsNullOrEmpty(_dataContainer.Values[configurationName].ToString()))
                    {
                        value = _dataContainer.Values[configurationName].ToString();
                    }
                    else
                    {
                        value = String.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in getting app data. " + ex.Message);
            }

            return value;
        }

        #endregion
    }
}
