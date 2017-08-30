using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HWCProximityWindowsApp.ProximityApp.Models
{
    #region Utility models

    /// <summary>
    /// Utility methods
    /// </summary>
    public class Utility
    {
        #region Public methods

        /// <summary>
        /// Shows app's personalized MessageBox.
        /// </summary>
        /// <param name="message">Message to be shown</param>
        /// <param name="title">Title to be shown, shows default title on empty.</param>
        /// <returns></returns>
        public static async Task MessageBoxShowAsync(string message, string title = "")
        {
            try
            { 
                string titleStr = string.IsNullOrEmpty(title) ? "HWConscious" : title;
                var messageDialog = new MessageDialog(message, titleStr);
                //messageDialog.CancelCommandIndex = 1;
                await messageDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in showing MessageBox. " + ex.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Provides BitmapImage from local or remote source.
    /// </summary>
    public static class BitmapImageProvider
    {
        #region Public methods

        /// <summary>
        /// Provides BitmapImage from a local source.
        /// </summary>
        /// <param name="localImageFileName">Image file name from local source</param>
        /// <param name="imageFolder">Source storage folder that contains the image file</param>
        /// <returns>Local source BitmapImage</returns>
        public static async Task<BitmapImage> GetImageFromLocalSourceAsync(string localImageFileName, StorageFolder imageFolder)
        {
            BitmapImage image = null;
            try
            {
                StorageFile imageFile = await imageFolder.GetFileAsync(localImageFileName);
                IRandomAccessStream imageFileStream = await imageFile.OpenAsync(FileAccessMode.Read);
                image = new BitmapImage();
                await image.SetSourceAsync(imageFileStream);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in generating BitmapImage from local source. " + ex.Message);
            }
            return image;
        }

        /// <summary>
        /// Provides BitmapImage from a remote source.
        /// </summary>
        /// <param name="remoteImageFileLink">Image file link from remote source</param>
        /// <returns>Remote source BitmapImage</returns>
        public static BitmapImage GetImageFromRemoteSource(string remoteImageFileLink)
        {
            BitmapImage image = null;
            try
            {
                image = new BitmapImage();
                image.UriSource = new Uri(remoteImageFileLink, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in generating BitmapImage from remote source. " + ex.Message);
            }
            return image;
        }

        /// <summary>
        /// Provides BitmapImage from a remote source, sets for the default image on failure.
        /// </summary>
        /// <param name="remoteImageFileLink">Image file link from remote source</param>
        /// <returns>Remote source or default BitmapImage</returns>
        public static BitmapImage GetImageFromRemoteSourceDefaultOnFailure(string remoteImageFileLink)
        {
            BitmapImage image = null;
            image = GetImageFromRemoteSource(remoteImageFileLink);
            if (image == null)
            {
                image = GetImageFromRemoteSource("ms-appx:///ProximityApp/MediaFiles/Logo_Hwc_Large.png");
            }
            return image;
        }

        #endregion
    }

    #endregion

    #region Representational models

    /// <summary>
    /// Representation of Event types.
    /// </summary>
    public enum EventType
    {
        None = 0,
        DisplayEndpoint_Touch = 1
    }

    /// <summary>
    /// Representation of MIME types.
    /// </summary>
    public enum MimeType
    {
        None = 0,
        ImagePng = 1,
        ImageJpeg = 2,
        ImageJpg = 3
    }

    /// <summary>
    /// Representation of Notification.
    /// </summary>
    public class Notification
    {
        public long NotificationID { get; set; }
        public long ClientSpotID { get; set; }
        public long DisplayEndpointID { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public int Timeout { get; set; }
        public bool Active { get; set; }
        public MimeType ContentMimeType { get; set; }
        public string ContentSubject { get; set; }
        public string ContentCaption { get; set; }
        public string ContentBody { get; set; }
    }

    #endregion
}
