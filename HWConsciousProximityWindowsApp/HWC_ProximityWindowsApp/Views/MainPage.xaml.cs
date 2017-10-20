using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.System;
using Windows.System.Power;
using Microsoft.Toolkit.Uwp;

using HWC_ProximityWindowsApp.ProximityApp.Models;

using Newtonsoft.Json;

namespace HWC_ProximityWindowsApp
{
    public sealed partial class MainPage : Page
    {
        #region Data members

        private long? _displayEndpointID { get; set; }
        private DispatcherTimer _notificationPullTimer { get; set; }
        private DispatcherTimer _notificationTimeoutTimer { get; set; }
        private RestClient _notificationRestClient { get; set; }
        private RestClient _eventRestClient { get; set; }
        private Notification _bufferedNotification { get; set; }
        private Dictionary<long, Notification> _receivedNotifications { get; set; }
        private bool _isUserEventConfirmationShowing = false;

        #endregion

        #region Initialize
        
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.VisibilityChanged += Window_VisibilityChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.LogAsync(Log.LoggingLevel.Information, "Navigated to Main Page");
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Validate the runtime
            if (await IsRuntimeValidatedAsync() == true)
            {
                // Show default panel
                _showDefaultPanel.Begin();

                // Create Notification timeout timer
                _notificationTimeoutTimer = new DispatcherTimer();
                _notificationTimeoutTimer.Tick += NotificationTimeoutTimer_Tick;

                // Set Notification video player's default configurations
                _notificationVideoPlayer.IsLooping = true;
                _notificationVideoPlayer.RealTimePlayback = true;
                _notificationVideoPlayer.MediaOpened += NotificationVideoPlayer_MediaOpened;

                // Cleanup expired video cache files
                await VideoCache.Instance.RemoveExpiredAsync();

                // Initialize REST client
                RestClientInitialize();

                // Initialize Notification pulling
                PullNotificationInitialize();

                InactiveProgressRing();
            }
            else
            {
                await Utility.MessageBoxShowAsync("Please reload the app after resolving the issue(s).");
                Application.Current.Exit();
            }
        }

        // Initialize client for REST calls
        private void RestClientInitialize()
        {
            if (_displayEndpointID != null)
            {
                var requestHeaders = new Dictionary<string, string>() { { "x-api-key", Constants.XApiKeyValue } };

                // Create the REST client for pulling Notifications
                _notificationRestClient = new RestClient(
                    RestClient.HttpVerb.GET,
                    Constants.RestApiEndpoint + "/display_endpoints/" + _displayEndpointID + "/notifications",
                    requestHeaders,
                    null,
                    null,
                    1000);  // 1 second timeout

                // Create the REST client for pushing Events
                _eventRestClient = new RestClient(
                    RestClient.HttpVerb.POST,
                    Constants.RestApiEndpoint + "/display_endpoints/" + _displayEndpointID + "/events",
                    requestHeaders,
                    null,
                    null,
                    4000);  // 4 seconds timeout
            }
        }

        // Initialize Notification pulling from cloud
        private void PullNotificationInitialize()
        {
            // Create dictionary for received Notifications
            _receivedNotifications = new Dictionary<long, Notification>();

            // Create & start the Notification pull timer
            _notificationPullTimer = new DispatcherTimer();
            _notificationPullTimer.Interval = TimeSpan.FromMilliseconds(Constants.NotificationPullFrequencyInMs);
            _notificationPullTimer.Tick += NotificationPullTimer_Tick;
            _notificationPullTimer.Start();         // Start Notification pull timer
            NotificationPullTimer_Tick(null, null); // Make a manual tick for Notification pull timer
        }

        #endregion

        #region Private methods

        // Notification pull timer ticked event
        private void NotificationPullTimer_Tick(object sender, object e)
        {
            PullNotificationAsync();
        }
        
        // Pull Notification from cloud
        private async void PullNotificationAsync()
        {
            try
            {
                // Make REST call to pull Notification
                string responseValue = await _notificationRestClient.MakeRequestAsync();
                Log.DebugLog("Notification pull response: " + responseValue);
                
                // Deserialize the response value as Notification
                var receivedNotification = JsonConvert.DeserializeObject<Notification>(responseValue);
                if (receivedNotification != null)
                {
                    _receivedNotifications[receivedNotification.NotificationID] = receivedNotification;
                }

                // Assign received Notification into the buffered Notification object
                if (_bufferedNotification == null)
                {
                    _bufferedNotification = receivedNotification;
                    LoadOrUnloadNotification(); // Fresh invoke
                }
                else
                {
                    _bufferedNotification = receivedNotification;
                }
            }
            catch (Exception ex)
            {
                _bufferedNotification = null;
                string log = "Error in REST call for Notification pulling. EXCEPTION: " + ex.Message;
                Log.DebugLog(log);
                Log.LogAsync(Log.LoggingLevel.Error, log);
            }
        }
        
        // Load or unload the buffered Notification
        private void LoadOrUnloadNotification()
        {
            // Hide notification-press panel
            _hideNotificationPressPanel.Begin();

            if (_bufferedNotification != null)
            {
                // There is a Notification to show
                // Clone the buffered Notification into an another object
                Notification notificationToShow = JsonConvert.DeserializeObject<Notification>(JsonConvert.SerializeObject(_bufferedNotification));
                // Show the Notification
                ShowNotificationAsync(notificationToShow);
            }
            else 
            {
                // There isn't any Notification to show
                // Clean-up Notification's UI control
                CleanUpNotificationControl();

                // Show default panel if not already visible
                if (_defaultContainerGrid.Visibility != Visibility.Visible)
                {
                    _showDefaultPanel.Begin();
                }
            }
        }

        // Show the Notification
        private async void ShowNotificationAsync(Notification notificationToShow)
        {
            try
            {
                if (!_isUserEventConfirmationShowing)
                {
                    // Validate the Notification content
                    if (notificationToShow != null && notificationToShow.Timeout > 0 && !string.IsNullOrEmpty(notificationToShow.ContentBody))
                    {
                        // Validate the Notification content type
                        if (notificationToShow.ContentMimeType == MimeType.ImagePng
                            || notificationToShow.ContentMimeType == MimeType.ImageJpeg
                            || notificationToShow.ContentMimeType == MimeType.ImageJpg
                            || notificationToShow.ContentMimeType == MimeType.VideoMp4)
                        {
                            // Start the Notification timeout timer with interval set to current Notification's timeout duration
                            _notificationTimeoutTimer.Interval = TimeSpan.FromMilliseconds(notificationToShow.Timeout * 1000); // Value (in second) multiplied with 1000 to convert it into milliseconds
                            _notificationTimeoutTimer.Start();  // Start Notification timeout timer

                            _hideVideoNotification.Begin();

                            switch (notificationToShow.ContentMimeType)
                            {
                                case MimeType.ImagePng:
                                case MimeType.ImageJpeg:
                                case MimeType.ImageJpg:
                                    // Assign the Notification image content to it's UI image control
                                    _notificationImageEx.Source = BitmapImageProvider.GetImageFromRemoteSource(notificationToShow.ContentBody);
                                    Log.DebugLog(">>>Show NotificationID (image content): " + notificationToShow.NotificationID);
                                    break;

                                case MimeType.VideoMp4:
                                    var videoUri = new Uri(notificationToShow.ContentBody);
                                    // Assign the Notification video content to it's UI video control
                                    _notificationVideoPlayer.SetPlaybackSource(await VideoCache.Instance.GetFromCacheAsync(videoUri));
                                    Log.DebugLog(">>>Show NotificationID (video content): " + notificationToShow.NotificationID);
                                    // Set video player's dimension for the video content
                                    SetVideoPlayerDimensionAsync(videoUri);
                                    break;
                            }

                            _notificationContainerGrid.Tag = notificationToShow.NotificationID;

                            // Show Notification panel
                            _hideDefaultPanel.Begin();
                            _showNotificationPanel.Begin();
                            // Show notification-press panel if there are any Coupon associated with the Notification
                            if (notificationToShow.Coupons?.Any() ?? false) { _showNotificationPressPanel.Begin(); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogAsync(Log.LoggingLevel.Error, "Notification content not loaded. EXCEPTION: " + ex.Message);
            }
        }

        // Notification timeout timer ticked event
        private void NotificationTimeoutTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _notificationTimeoutTimer.Stop();

            // Clean-up Notification's UI control
            CleanUpNotificationControl();

            // Recursive invoke
            LoadOrUnloadNotification();
        }

        // Push user event to cloud
        private async void PushUserEventAsync(long notificationID, EventType eventType)
        {
            // Validate the NotificationID and event type as 'DisplayEndpoint_Touch'
            if (notificationID > 0 && eventType == EventType.DisplayEndpoint_Touch)
            {
                _bufferedNotification = null;               // Clear the buffered Notification
                _isUserEventConfirmationShowing = true;     // Set to true
                _notificationTimeoutTimer.Stop();           // Stop Notification timeout timer
                _notificationPullTimer.Stop();              // Stop Notification pull timer

                try
                {
                    // Prepare content for REST request
                    _eventRestClient.UpdateContent("{\"Type\": \"DisplayEndpoint_Touch\"," + 
                                                    "\"EventAtTimestamp\": \"" + DateTime.UtcNow.ToString() + "\"," +
                                                    "\"SourceType\": \"Notification\"," +
                                                    "\"SourceID\": " + notificationID + "," +
                                                    "\"Message\": \"NotificationID " + notificationID + " touched on a DisplayEndpointID" + _displayEndpointID + "\"}");
                
                    // Make REST call to push user event
                    string responseValue = await _eventRestClient.MakeRequestAsync();

                    string log = "Event sent successfully for NotificationID: " + notificationID;
                    Log.DebugLog(log);
                    Log.LogAsync(Log.LoggingLevel.Information, log);

                    // Show user-event confirmation panel
                    _hideNotificationPanel.Begin();
                    _showUserEventConfirmationPanel.Begin();

                    // Clean-up Notification's UI control
                    CleanUpNotificationControl();

                    // Delay the thread with specified duration for keep showing user event confirmation
                    await Task.Delay(Constants.UserEventConfirmationDurationInMs);

                    // Hide user-event confirmation panel
                    _hideUserEventConfirmationPanel.Begin();
                }
                catch (Exception ex)
                {
                    string log = "Error in REST call for user event pushing (NotificationID: " + notificationID + "). EXCEPTION: " + ex.Message;
                    Log.DebugLog(log);
                    Log.LogAsync(Log.LoggingLevel.Error, log);
                }

                _bufferedNotification = null;               // Clear the buffered Notification
                _isUserEventConfirmationShowing = false;    // Set to false
                _notificationPullTimer.Start();             // Start Notification pull timer
                NotificationPullTimer_Tick(null, null);     // Make a manual tick for Notification pull timer
            }
        }
        
        #endregion

        #region Helper methods
        
        private async Task<bool> IsRuntimeValidatedAsync()
        {
            bool retValue = true;

            // Check if battery saving mode is on; App may not receive push notifications if it's on
            if (!await CheckForEnergySavingAsync())
            {
                InactiveProgressRing();
                await Utility.MessageBoxShowAsync("Checking for battery saving not done");
                retValue = false;
            }

            // Check if internet connection is available
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                InactiveProgressRing();
                await Utility.MessageBoxShowAsync("Internet is not available");
                retValue = false;
            }

            // Check if DisplayEndpointID is configured and valid
            LoadDisplayEndpointIDFromSettings();
            if (_displayEndpointID == null || _displayEndpointID < 1)
            {
                InactiveProgressRing();
                if (await ConfigureDisplayEndpointIDAsync())
                {
                    await Utility.MessageBoxShowAsync("DisplayEndpointID configured successfully. App reload needed.");
                }
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
                object dontAskSetting = AppData.GetAppData(ConfigurationNames.DontAskAgainEnergySavingSetting);
                bool dontAskAgain = dontAskSetting == null ? false : Convert.ToBoolean(dontAskSetting);

                // Check if battery saver is on and that it's okay to raise dialog
                if ((PowerManager.EnergySaverStatus == EnergySaverStatus.On) && (dontAskAgain == false))
                {
                    ContentDialogResult dialogResult = await _saveEnergyContentDialog.ShowAsync();
                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        // Launch battery saver settings (settings are available only when a battery is present)
                        await Launcher.LaunchUriAsync(new Uri("ms-settings:batterysaver-settings"));
                    }

                    // Save reminder preference
                    if (_dontAskAgainEnergySavingCheckBox.IsChecked == true)
                    {
                        AppData.SetAppData(ConfigurationNames.DontAskAgainEnergySavingSetting, true.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogAsync(Log.LoggingLevel.Error, "Energy saving setting cheking not done. EXCEPTION: " + ex.Message);
                retValue = false;
            }

            return retValue;
        }

        private async Task<bool> ConfigureDisplayEndpointIDAsync()
        {
            bool retValue = false;
            try
            {
                ContentDialogResult dialogResult = await _configureDisplayEndpointIDContentDialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    try
                    {
                        _displayEndpointID = Convert.ToInt64(_configureDisplayEndpointIDTextBox.Text);
                        if (_displayEndpointID != null && _displayEndpointID > 0)
                        {
                            AppData.SetAppData(ConfigurationNames.DisplayEndpointID, _displayEndpointID.Value.ToString());
                            retValue = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogAsync(Log.LoggingLevel.Error, "Invalid DisplayEndpointID provided. EXCEPTION: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogAsync(Log.LoggingLevel.Error, "Error in DisplayEndpointID configuration. EXCEPTION: " + ex.Message);
            }
            return retValue;
        }

        private void LoadDisplayEndpointIDFromSettings()
        {
            try
            {
                _displayEndpointID = Convert.ToInt64(AppData.GetAppData(ConfigurationNames.DisplayEndpointID));
            }
            catch (Exception ex)
            {
                Log.LogAsync(Log.LoggingLevel.Error, "Error in loading DisplayEndpointID. EXCEPTION: " + ex.Message);
            }
        }

        private void InactiveProgressRing()
        {
            _progressRing.IsActive = false;
        }

        private void CleanUpNotificationControl()
        {
            _notificationImageEx.Source = null;
            _notificationVideoPlayer.Source = null;
            _notificationContainerGrid.Tag = null;
        }

        private async void SetVideoPlayerDimensionAsync(Uri videoUri)
        {
            if (videoUri != null)
            {
                try
                {
                    var cacheFile = await VideoCache.Instance.GetFileFromCacheAsync(videoUri);
                    var tempFile = await cacheFile.CopyAsync(ApplicationData.Current.TemporaryFolder, cacheFile.Name + ".mp4", NameCollisionOption.ReplaceExisting);
                    
                    var properties = await tempFile.Properties.RetrievePropertiesAsync(new List<string>() { "System.Video.FrameWidth", "System.Video.FrameHeight" });
                    _notificationVideoPlayer.Width = (uint)properties["System.Video.FrameWidth"];
                    _notificationVideoPlayer.Height = (uint)properties["System.Video.FrameHeight"];
                    (_notificationVideoPlayer.Parent as Viewbox).MaxWidth = _notificationVideoPlayer.Width;
                    (_notificationVideoPlayer.Parent as Viewbox).MaxHeight = _notificationVideoPlayer.Height;

                    await tempFile.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Log.LogAsync(Log.LoggingLevel.Error, "Unable to set dimension for video player. URI: " + videoUri.ToString() + " EXCEPTION: " + ex.Message);
                }
            }
        }

        private void NotificationContainerGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            { 
                _notificationPointerDownEffect.Begin();
            }
            catch (Exception ex)
            {
                string log = "Notification panel pointer-press event error. EXCEPTION: " + ex.Message;
                Log.DebugLog(log);
                Log.LogAsync(Log.LoggingLevel.Error, log);
            }

            if (!_isUserEventConfirmationShowing)
            {
                try
                {
                    long notificationID = (long)((Grid)sender).Tag; // Get the NotificationID from Notification's UI panel control
                    if (_receivedNotifications[notificationID]?.Coupons?.Any() ?? false)
                    {
                        PushUserEventAsync(notificationID, EventType.DisplayEndpoint_Touch);
                    }
                }
                catch (Exception ex)
                {
                    string log = "Invalid NotificationID at Notification panel pointer-press event. EXCEPTION: " + ex.Message;
                    Log.DebugLog(log);
                    Log.LogAsync(Log.LoggingLevel.Error, log);
                }
            }
        }

        private void NotificationContainerGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                _notificationPointerDownEffect.Stop();
                _notificationPointerUpEffect.Begin();
            }
            catch (Exception ex)
            {
                string log = "Notification panel pointer-release event error. EXCEPTION: " + ex.Message;
                Log.DebugLog(log);
                Log.LogAsync(Log.LoggingLevel.Error, log);
            }
        }
        
        private void NotificationVideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            _showVideoNotification.Stop();
            _showVideoNotification.Begin();
        }

        private void Window_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            var appCurrentView = ApplicationView.GetForCurrentView();
            // Enter into FullScreen mode
            if (appCurrentView.AdjacentToLeftDisplayEdge == true && appCurrentView.AdjacentToRightDisplayEdge == true && appCurrentView.IsFullScreenMode == false)
            {
                appCurrentView.TryEnterFullScreenMode();
            }
        }

        #endregion
    }
}
