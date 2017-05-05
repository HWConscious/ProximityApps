using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HWCProximityWindowsApp.Common.HwcClasses
{
    public class HwcUtility
    {
        // Show Message Box
        public static async Task MessageBoxShowAsync(string message, string title = "")
        {
            string titleStr = string.IsNullOrEmpty(title) ? "HWConscious" : title;
            var messageDialog = new MessageDialog(message, titleStr);
            //messageDialog.CancelCommandIndex = 1;
            await messageDialog.ShowAsync();
            return;
        }
    }

    public static class HwcSetImage
    {
        public static BitmapImage GetImageSource(string remoteImageFileLink)
        {
            BitmapImage imageSource = null;

            imageSource = GetImageSourceFromRemote(remoteImageFileLink); imageSource = null;
            if (imageSource == null)
            {
                imageSource = GetImageSourceFromRemote("ms-appx:///Common/HwcMediaFiles/Logo_Hwc_Large.png");
            }

            return (imageSource);
        }

        public static BitmapImage GetImageSourceFromRemote(string remoteImageFileLink)
        {
            BitmapImage imageSource = null;
            try
            {
                imageSource = new BitmapImage();
                imageSource.UriSource = new Uri(remoteImageFileLink, UriKind.Absolute);
            }
            catch(Exception ex)
            {
                HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Error in getting image source from remote. EXCEPTION: " + ex.Message);
            }
            return imageSource;
        }

        public static async Task<BitmapImage> GetImageSourceFromLocalAsync(string imageFileName, StorageFolder imageFolder)
        {
            BitmapImage imageSource = null;
            try
            {
                StorageFile image = await imageFolder.GetFileAsync(imageFileName);
                IRandomAccessStream imageFileInputStream = await image.OpenAsync(Windows.Storage.FileAccessMode.Read);

                imageSource = new BitmapImage();
                await imageSource.SetSourceAsync(imageFileInputStream);
            }
            catch (Exception ex)
            {
                HwcLog.LogAsync(HwcLog.LoggingLevel.Error, "Error in getting image source from local. EXCEPTION: " + ex.Message);
            }
            return imageSource;
        }
    }
}
