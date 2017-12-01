using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;

using Microsoft.Toolkit.Uwp.UI;

namespace HWC_ProximityWindowsApp.ProximityApp.Models
{
    /// <summary>
    /// Provides methods and tools to cache videos (in app's temp folder).
    /// </summary>
    public class VideoCache : CacheBase<MediaSource>
    {
        #region Data members

        /// <summary>
        /// Private singleton field.
        /// </summary>
        private static VideoCache _instance;

        /// <summary>
        /// Gets public singleton property.
        /// </summary>
        public static VideoCache Instance => _instance ?? (_instance = new VideoCache() { MaintainContext = true, CacheDuration = TimeSpan.FromDays(Constants.VideoNotificationCacheDurationInDays) });

        #endregion

        #region Protected methods

        /// <summary>
        /// Cache specific hooks to process items from HTTP response
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <param name="initializerKeyValues">key value pairs used when initializing instance of generic type</param>
        /// <returns>Media source</returns>
        protected override Task<MediaSource> InitializeTypeAsync(Stream stream, List<KeyValuePair<string, object>> initializerKeyValues = null)
        {
            return Task.Run(() => MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/mp4"));
        }

        /// <summary>
        /// Cache specific hooks to process items from HTTP response
        /// </summary>
        /// <param name="baseFile">storage file</param>
        /// <param name="initializerKeyValues">key value pairs used when initializing instance of generic type</param>
        /// <returns>Media source</returns>
        protected override Task<MediaSource> InitializeTypeAsync(StorageFile baseFile, List<KeyValuePair<string, object>> initializerKeyValues = null)
        {
            return Task.Run(() => MediaSource.CreateFromStorageFile(baseFile));
        }

        #endregion
    }
}
