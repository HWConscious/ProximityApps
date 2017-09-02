using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HWC_ProximityWindowsApp.ProximityApp.Models
{
    /// <summary>
    /// Representation of REST request method types.
    /// </summary>
    public enum HttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    /// <summary>
    /// REST client interface
    /// </summary>
    public class RestClient
    {
        #region Data members

        public HttpVerb HttpMethod { get; set; }
        public string EndPoint { get; set; }
        public ICredentials Credentials { get; set; }
        public WebHeaderCollection Headers { get; set; }
        public string ContentType { get; set; }
        public string RequestContent { get; set; }
        public int? TimeoutInMs { get; set; }

        #endregion

        #region Initialize

        /// <summary>
        /// REST client constructor
        /// </summary>
        public RestClient()
        {
            HttpMethod = HttpVerb.GET;
            EndPoint = string.Empty;
            Credentials = null;
            Headers = new WebHeaderCollection();
            ContentType = "application/json";
            RequestContent = string.Empty;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Makes RESTful HTTP request
        /// </summary>
        /// <returns>Response of the request (could be JSON, XML or HTML etc. serialized into string)</returns>
        public async Task<string> MakeRequestAsync()
        {
            HttpWebRequest webRequest = null;

            // Create a HttpWebRequest instance for web request
            try
            {
                webRequest = (HttpWebRequest)WebRequest.Create(EndPoint);
                webRequest.Method = HttpMethod.ToString();
                webRequest.Credentials = Credentials;
                webRequest.Headers = Headers;
                webRequest.ContentType = ContentType;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to create HttpWebRequest handler. " + ex.Message);
            }

            if (webRequest != null)
            {
                return await ProcessWebRequestAsync(webRequest);
            }

            return null;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Processes HTTP web request
        /// </summary>
        /// <returns>Response of the request</returns>
        private async Task<string> ProcessWebRequestAsync(HttpWebRequest webRequest)
        {
            string responseValue = null;

            if (webRequest != null)
            {
                if (HttpMethod != HttpVerb.GET)
                {
                    // Write request content into web request stream
                    try
                    {
                        using (Stream requestStream = await webRequest.GetRequestStreamAsync())
                        {
                            byte[] contentInBytes = Encoding.UTF8.GetBytes(RequestContent);
                            requestStream.Write(contentInBytes, 0, contentInBytes.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error in request content. " + ex.Message);
                    }
                }

                // Make the web request and process the response
                HttpWebResponse webResponse = null;
                try
                {
                    // Set a timer to abort the web requset on timeout (if any)
                    CancellationTokenSource abortWebRequestTaskCancellationTokenSource = null;
                    if (TimeoutInMs != null && TimeoutInMs > -1)
                    {
                        abortWebRequestTaskCancellationTokenSource = new CancellationTokenSource();
                        AbortWebRequestOnTimeoutAsync(webRequest, abortWebRequestTaskCancellationTokenSource.Token);
                    }

                    // Make the web request
                    webResponse = (HttpWebResponse)await webRequest.GetResponseAsync();

                    // Cancel the aborting web request operation now as it made the full cycle before the timeout timer got hit
                    abortWebRequestTaskCancellationTokenSource?.Cancel();

                    // Process the response stream
                    using (Stream webResponseStream = webResponse.GetResponseStream())
                    {
                        if (webResponseStream != null)
                        {
                            // Read the response stream into string
                            using (StreamReader reader = new StreamReader(webResponseStream))
                            {
                                responseValue = await reader.ReadToEndAsync();
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    // Handle the different web exceptions
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                        if (statusCode != HttpStatusCode.OK)
                        {
                            throw new Exception(((int)statusCode).ToString());
                        }
                    }
                    else if (ex.Status == WebExceptionStatus.RequestCanceled)
                    {
                        throw new Exception("Web request was timed out, hence aborted.");
                    }
                    else
                    {
                        throw new Exception("Error in RESTful HTTP request (" + ex.Status.ToString() + "). " + ex.Message);
                    }
                }

                if (webResponse != null)
                {
                    webResponse.Dispose();
                }
            }

            return responseValue;
        }

        /// <summary>
        /// Set a timer to abort the web requset on timeout (if any)
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="cancellationToken"></param>
        private async void AbortWebRequestOnTimeoutAsync(HttpWebRequest webRequest, CancellationToken cancellationToken)
        {
            if (webRequest != null)
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(TimeoutInMs ?? 0); // Set timeout timer
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        webRequest.Abort(); // Abort the web request
                    }
                }, cancellationToken); // Set the timeout Task with cancellation token
            }
        }

        #endregion
    }
}
