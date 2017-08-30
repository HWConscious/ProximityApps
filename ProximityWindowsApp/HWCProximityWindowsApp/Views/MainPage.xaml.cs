using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Windows.System.Power;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Controls;

using HWCProximityWindowsApp.ProximityApp.Models;

using Newtonsoft.Json;

namespace HWCProximityWindowsApp
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
                // Initialize REST client
                RestClientInitialize();

                // Initialize Notification pulling
                PullNotificationInitialize();
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
                // Create the REST client for pulling Notifications
                _notificationRestClient = new RestClient();
                _notificationRestClient.HttpMethod = HttpVerb.GET;
                _notificationRestClient.EndPoint = Constants.RestApiEndpoint + "/display_endpoints/" + _displayEndpointID + "/notifications";
                _notificationRestClient.Headers["x-api-key"] = Constants.XApiKeyValue;


                // Create the REST client for pushing Events
                _eventRestClient = new RestClient();
                _eventRestClient.HttpMethod = HttpVerb.POST;
                _eventRestClient.EndPoint = Constants.RestApiEndpoint + "/display_endpoints/" + _displayEndpointID + "/events";
                _eventRestClient.Headers["x-api-key"] = Constants.XApiKeyValue;
            }
        }

        // Initialize Notification pulling from cloud
        private void PullNotificationInitialize()
        {
            // Create & start the Notification pull timer
            _notificationPullTimer = new DispatcherTimer();
            _notificationPullTimer.Interval = TimeSpan.FromMilliseconds(Constants.NotificationPullFrequencyInMs);
            _notificationPullTimer.Tick += NotificationPullTimer_Tick;
            _notificationPullTimer.Start();         // Start Notification pull timer
            NotificationPullTimer_Tick(null, null); // Make a manual tick for Notification pull timer
            InactiveProgressRing();
        }

        #endregion

        #region Private methods

        // Notification pull timer ticked event
        private void NotificationPullTimer_Tick(object sender, object e)
        {
            if (!_isUserEventConfirmationShowing)
            {
                PullNotificationAsync();
            }
        }
        
        // Pull Notification from cloud
        private async void PullNotificationAsync()
        {
            try
            {
                // Make REST call to pull Notification
                var startTime = DateTime.Now;
                string responseValue = await _notificationRestClient.MakeRequestAsync();
                var endTime = DateTime.Now;

                // Discard the response if the request took more than 1 second for pulling
                if (endTime.Subtract(startTime).TotalMilliseconds > 1000)
                {
                    string log = "Notification pulling request took more than 1 second, hence discarded.";
                    Log.DebugLog(log);
                    Log.LogAsync(Log.LoggingLevel.Warning, log);
                }
                else
                {
                    Log.DebugLog("Notification pull response: " + responseValue);

                    // Deserialize the response value into buffered Notification object
                    if (_bufferedNotification == null)
                    {
                        _bufferedNotification = JsonConvert.DeserializeObject<Notification>(responseValue);
                        LoadOrUnloadNotification(); // Fresh invoke
                    }
                    else
                    {
                        _bufferedNotification = JsonConvert.DeserializeObject<Notification>(responseValue);
                    }
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
            if (_bufferedNotification != null)
            {
                // There is a Notification to show
                // Clone the buffered Notification into an another object
                Notification notificationToShow = JsonConvert.DeserializeObject<Notification>(JsonConvert.SerializeObject(_bufferedNotification));
                // Show the Notification
                ShowNotification(notificationToShow);
            }
            else 
            {
                // There isn't any Notification to show
                // Clean-up Notification's UI control
                CleanUpNotificationControl();
                // Show the default grid
                ShowDefaultContainerGrid();
            }
        }

        // Show the Notification
        private void ShowNotification(Notification notificationToShow)
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
                            || notificationToShow.ContentMimeType == MimeType.ImageJpg)
                        {
                            // Create & start the Notification timeout timer with interval set to current Notification's timeout duration
                            _notificationTimeoutTimer = new DispatcherTimer();
                            _notificationTimeoutTimer.Interval = TimeSpan.FromMilliseconds(notificationToShow.Timeout * 1000); // Value (in second) multiplied with 1000 to convert it into milliseconds
                            _notificationTimeoutTimer.Tick += NotificationTimeoutTimer_Tick;
                            _notificationTimeoutTimer.Start();  // Start Notification timeout timer

                            // Assign the Notification image to it's UI image control
                            string remoteImageHttpLink = notificationToShow.ContentBody;
                            _notificationImageEx.Source = BitmapImageProvider.GetImageFromRemoteSource(remoteImageHttpLink);
                            _notificationImageEx.Tag = notificationToShow.NotificationID;
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
            // Obsolete timer; clear it.
            var thisTimer = (DispatcherTimer)sender;
            thisTimer.Stop();
            thisTimer = null;

            if (!_isUserEventConfirmationShowing)
            {
                LoadOrUnloadNotification(); // Recursive invoke
            }
        }

        // Push user event to cloud
        private async void PushUserEventAsync(long notificationID, EventType eventType)
        {
            // Validate the NotificationID and event type as 'DisplayEndpoint_Touch'
            if (notificationID > 0 && eventType == EventType.DisplayEndpoint_Touch)
            {
                _bufferedNotification = null;               // Clear the buffered Notification
                _isUserEventConfirmationShowing = true;     // Set to true
                _notificationPullTimer.Stop();              // Stop Notification pull timer
                NotificationTimeoutTimer_Tick(_notificationTimeoutTimer, null); // Make a manual tick for Notification timeout timer

                try
                {
                    // Prepare content for REST request
                    _eventRestClient.RequestContent = "{\"Type\": \"DisplayEndpoint_Touch\"," + 
                                                        "\"EventAtTimestamp\": \"" + DateTime.UtcNow.ToString() + "\"," +
                                                        "\"SourceType\": \"Notification\"," +
                                                        "\"SourceID\": " + notificationID + "," +
                                                        "\"Message\": \"NotificationID " + notificationID + " touched on a DisplayEndpointID" + _displayEndpointID + "\"}";
                
                    // Make REST call to push user event
                    string responseValue = await _eventRestClient.MakeRequestAsync();

                    string log = "Event sent successfully for NotificationID: " + notificationID;
                    Log.DebugLog(log);
                    Log.LogAsync(Log.LoggingLevel.Information, log);

                    ShowUserEventConfirmation();            // Show user event confirmation
                    CleanUpNotificationControl();           // Clean-up Notification's UI control

                    // Delay the thread with specified duration for keep showing user event confirmation
                    await Task.Delay(Constants.UserEventConfirmationDurationInMs);

                    HideUserEventConfirmation();            // Hide user event confirmation
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
            _notificationImageEx.Tag = null;
        }

        private void ShowDefaultContainerGrid()
        {
            if (_defaultContainerGrid.Visibility != Visibility.Visible)
            {
                _defaultContainerGrid.Visibility = Visibility.Visible;
                _showDefaultContainerGrid.Begin();
            }
        }

        private void HideDefaultContainerGrid()
        {
            _defaultContainerGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowUserEventConfirmation()
        {
            _notificationContainerGrid.Visibility = Visibility.Collapsed;
            _userEventConfirmationContainerGrid.Visibility = Visibility.Visible;
            _showUserEventConfirmation.Begin();
        }

        private void HideUserEventConfirmation()
        {
            _hideUserEventConfirmation.Begin();
            _notificationContainerGrid.Visibility = Visibility.Visible;
        }
        
        private void NotificationImageEx_ImageExOpened(object sender, ImageExOpenedEventArgs e)
        {
            HideDefaultContainerGrid();
        }
        
        private void NotificationImageEx_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pointerDownNotificationImageEx.Begin();

            if (!_isUserEventConfirmationShowing)
            {
                try
                {
                    long notificationID = (long)((ImageEx)sender).Tag; // Get the NotificationID from Notification's UI image control
                    PushUserEventAsync(notificationID, EventType.DisplayEndpoint_Touch);
                }
                catch (Exception ex)
                {
                    string log = "Invalid NotificationID at image tap event. EXCEPTION: " + ex.Message;
                    Log.DebugLog(log);
                    Log.LogAsync(Log.LoggingLevel.Error, log);
                }
            }
        }

        private void NotificationImageEx_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                _pointerDownNotificationImageEx.Stop();
                _pointerUpNotificationImageEx.Begin();
            }
            catch (Exception ex)
            {
                Log.LogAsync(Log.LoggingLevel.Error, "Notification image tap event release error. EXCEPTION: " + ex.Message);
            }
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
