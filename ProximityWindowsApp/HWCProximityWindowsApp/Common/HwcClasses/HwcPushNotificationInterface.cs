using System;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;

namespace HWCProximityWindowsApp.Common.HwcClasses
{
    public class HwcPushNotificationInterface
    {
        // Notification channel reference
        public PushNotificationChannel NotificationChannel { get; set; }

        // Request a new notification channel
        public async Task<bool> RequestNotificationChannelAsync()
        {
            bool retValue = false;
            bool isNewChannelSentToCloud = false;
            PushNotificationChannel newChannel = null;

            // Create a new push notification channel
            try
            {
                newChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create a channel. " + ex.Message);

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

        // Get a 'Raw' type push notification for a specific channel
        public RawNotification GetRawNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            RawNotification notification = null;
            if (sender != null && sender == this.NotificationChannel && args != null)
            {
                if (args.NotificationType == PushNotificationType.Raw)
                {
                    notification = args.RawNotification;
                }
                else
                {
                    throw new Exception("Notification is not of 'Raw' type");
                }
            }
            else
            {
                throw new Exception("Notification channel is invalid");
            }
            return notification;
        }
    }
}
