using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Windows.System.Power;
using Windows.Networking.PushNotifications;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Controls;

using HWCProximityWindowsApp.Common.HwcClasses;

namespace HWCProximityWindowsApp
{
    public sealed partial class MainPage : Page
    {
        #region Data members

        private HwcPushNotificationInterface PushNotificationInterface { get; set; }

        #endregion

        #region Initialize

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HwcLog.LogAsync(HwcLog.LoggingLevel.Information, "Navigated to Main Page");
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (await IsRuntimeValidatedAsync() == true)
            {
                // Initiate push notification
                PushNotificationInitializeAsync();
            }
            else
            {
                await HwcUtility.MessageBoxShowAsync("Please restart the app after resolving the issue(s).");

            }
        }

        #endregion

        #region Private methods

        // Method to initialize Push Notification
        private async void PushNotificationInitializeAsync()
        {
            PushNotificationChannel notificationChannel = null;

            // Initialize push notification interface
            if (this.PushNotificationInterface == null)
            {
                this.PushNotificationInterface = new HwcPushNotificationInterface();

                try
                {
                    if (await this.PushNotificationInterface.RequestNotificationChannelAsync())
                    {
                        notificationChannel = this.PushNotificationInterface.NotificationChannel;
                    }
                    else
                    {
                        HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Notification channel is  not created");
                    }
                }
                catch (Exception ex)
                {
                    HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Error in creating new notification channel. EXCEPTION: " + ex.Message);
                }

                if (notificationChannel != null)
                {
                    // New notification channel created
                    HwcLog.LogAsync(HwcLog.LoggingLevel.Information, "New notification channel created. Channel URI: " + notificationChannel.Uri);

                    notificationChannel.PushNotificationReceived += NotificationChannel_PushNotificationReceived;

                    TriggerFirstPushNotificationForNewChannelFromCloudService();
                }
            }
        }

        // Method triggers the first push notification for a new channel from cloud service
        private void TriggerFirstPushNotificationForNewChannelFromCloudService()
        {
            // TODO: Trigger first push notfication from cloud

            // TEMP CODE Start
            // Delete this code after writing first notification triggering code
            string remoteImageHttpLink = "https://valuestockphoto.com/freehighresimages/roast_veg_DSC2834.jpg";
            ShowARemoteImage(remoteImageHttpLink);
            // TEMP CODE End
        }

        // Method invoked when a new push notification received at the channel
        private void NotificationChannel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            RawNotification rawNotification = null;
            if (this.PushNotificationInterface != null)
            {
                try
                {
                    rawNotification = this.PushNotificationInterface.GetRawNotification(sender, args);
                }
                catch (Exception ex)
                {
                    HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Error in getting Raw notification. EXCEPTION: " + ex.Message);
                }

                if (rawNotification != null)
                {
                    HwcLog.LogAsync(HwcLog.LoggingLevel.Information, "New push notification received. CONTENT: " + rawNotification.Content);

                    string remoteImageHttpLink = rawNotification.Content;
                    ShowARemoteImage(remoteImageHttpLink);
                }
            }
        }

        private void ShowARemoteImage(string remoteImageHttpLink)
        {
            try
            {
                imageExControl1.Source = HwcSetImage.GetImageSourceFromRemote(remoteImageHttpLink);
                imageExControl1.ImageExOpened += imageExControl1_ImageExOpened;
            }
            catch (Exception ex)
            {
                HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Image not loaded. EXCEPTION: " + ex.Message);
            }
        }

        private void imageExControl1_ImageExOpened(object sender, ImageExOpenedEventArgs e)
        {
            gridContainer.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Helpers

        private async Task<bool> IsRuntimeValidatedAsync()
        {
            bool retValue = true;

            // Check if battery saving mode is on;
            // App may not receive push notifications if it's on
            if (!await CheckForEnergySavingAsync())
            {
                InactiveProgressRing();
                await HwcUtility.MessageBoxShowAsync("Checking for battery saving not done");
                retValue = false;
            }
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                InactiveProgressRing();
                await HwcUtility.MessageBoxShowAsync("Internet is not available");
                retValue = false;
            }

            return retValue;
        }

        private async Task<bool> CheckForEnergySavingAsync()
        {
            bool retValue = true;

            try
            {
                //Get reminder preference from LocalSettings
                bool dontAskAgain;

                object dontAskSetting = HwcApplicationData.GetApplicationData(HwcAppConfigurationName.DontAskAgainEnergySavingSetting);
                if (dontAskSetting == null)
                {
                    dontAskAgain = false;
                }
                else
                {
                    dontAskAgain = Convert.ToBoolean(dontAskSetting);
                }

                // Check if battery saver is on and that it's okay to raise dialog
                if ((PowerManager.EnergySaverStatus == EnergySaverStatus.On) && (dontAskAgain == false))
                {
                    ContentDialogResult dialogResult = await saveEnergyDialog.ShowAsync();
                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        // Launch battery saver settings (settings are available only when a battery is present)
                        await Launcher.LaunchUriAsync(new Uri("ms-settings:batterysaver-settings"));
                    }

                    // Save reminder preference
                    if (dontAskAgainEnergySavingBox.IsChecked == true)
                    {
                        HwcApplicationData.SetApplicationData(HwcAppConfigurationName.DontAskAgainEnergySavingSetting, true.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Energy saving setting cheking not done. EXCEPTION: " + ex.Message);
                retValue = false;
            }

            return retValue;
        }

        private void InactiveProgressRing()
        {
            progressRing.IsActive = false;
        }

        #endregion
    }
}
