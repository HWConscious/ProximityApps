using System;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;

namespace HWC_ProximityWindowsApp.ProximityApp.Models
{
    /// <summary>
    /// PushNotification interface
    /// </summary>
    public class PushNotificationInterface
    {
        #region Data members

        /// <summary>
        /// Notification channel reference
        /// </summary>
        public PushNotificationChannel NotificationChannel { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Requests for push notification channel.
        /// </summary>
        /// <returns>Notification channel request status</returns>
        public async Task<bool> RequestNotificationChannelAsync()
        {
            bool retValue = false;
            bool isNewChannelSentToCloud = false;

            // Create a new push notification channel
            PushNotificationChannel newChannel = null;
            try
            {
                newChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in creating notification channel. " + ex.Message);
                // TODO: Try three attempts with a 10-second delay between each unsuccessful attempt.
            }

            // Send the new channel to AWS cloud service
            if (newChannel != null)
            {
                if (newChannel != this.NotificationChannel)
                {
                    // TODO: Send the new channel to cloud service

                    // TEMP CODE Start
                    isNewChannelSentToCloud = true; // Set it to true on successful send to cloud service
                    // TEMP CODE End

                    // TODO: Try five attempts with a 10-second delay between each unsuccessful attempt.
                }
                else
                {
                    throw new Exception("New channel is same as the previous channel.");
                }
            }

            // Update the local channel reference
            if (isNewChannelSentToCloud)
            {
                this.NotificationChannel = newChannel;
                retValue = true;
            }

            return retValue;
        }

        /// <summary>
        /// Gets a RawNotification for a specific channel.
        /// </summary>
        /// <param name="sender">PushNotification channel as sender</param>
        /// <param name="args">Event args</param>
        /// <returns>Push notofication as RawNotification</returns>
        public RawNotification GetRawNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            RawNotification rawNotification = null;
            if (sender != null && sender == this.NotificationChannel && args != null)
            {
                if (args.NotificationType == PushNotificationType.Raw)
                {
                    rawNotification = args.RawNotification;
                }
                else
                {
                    throw new Exception("The push notification is not a RawNotification type.");
                }
            }
            else
            {
                throw new Exception("The push notification channel is invalid.");
            }
            return rawNotification;
        }

        #endregion
    }
}
