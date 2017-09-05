using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HWC_ProximityWindowsApp.ProximityApp.Models
{
    /// <summary>
    /// REST client
    /// </summary>
    public class RestClient
    {
        #region Data members

        private readonly HttpVerb _httpMethod;
        private readonly Uri _endPoint;
        private readonly Dictionary<string, string> _headers;
        private readonly string _contentType;
        private string _content { get; set; }
        private int? _timeoutInMs { get; set; }

        /// <summary>
        /// REST request methods.
        /// </summary>
        public enum HttpVerb
        {
            GET,
            POST,
            PUT,
            DELETE
        }

        #endregion

        #region Initialize

        /// <summary>
        /// REST client constructor
        /// </summary>
        /// <param name="httpMethod">Method to be used for REST call</param>
        /// <param name="endPoint">REST endpoint</param>
        /// <param name="headers">HTTP request headers</param>
        /// <param name="contentType">Request content type. Default: "application/json"</param>
        /// <param name="content">Request content</param>
        /// <param name="timeoutInMs">Request timeout value in millisecond</param>
        public RestClient(HttpVerb httpMethod, string endPoint, Dictionary<string, string> headers = null, string contentType = null, string content = null, int? timeoutInMs = null)
        {
            _httpMethod = httpMethod;
            _endPoint = new Uri(endPoint, UriKind.Absolute);
            _headers = headers;
            _contentType = contentType ?? "application/json";
            _content = content ?? string.Empty;
            _timeoutInMs = timeoutInMs;
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
                webRequest = (HttpWebRequest)WebRequest.Create(_endPoint);
                webRequest.Method = _httpMethod.ToString();
                webRequest.ContentType = _contentType;
                if (_headers?.Count > 0)
                {
                    foreach (KeyValuePair<string, string> header in _headers)
                    {
                        webRequest.Headers[header.Key] = header.Value;
                    }
                }
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

        /// <summary>
        /// Update request content
        /// </summary>
        /// <param name="content">Request content</param>
        /// <returns></returns>
        public bool UpdateContent(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                _content = content;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update request timeout
        /// </summary>
        /// <param name="timeoutInMs">Timeout value in millisecond</param>
        /// <returns></returns>
        public bool UpdateTimeout(int? timeoutInMs)
        {
            if (timeoutInMs != null && timeoutInMs < 0)
            {
                return false;
            }
            _timeoutInMs = timeoutInMs;
            return true;
        }

        /// <summary>
        /// Get Endpoint of the REST client
        /// </summary>
        /// <returns></returns>
        public string GetEndpoint()
        {
            return _endPoint.AbsolutePath;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Processes HTTP web request
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns>Response of the request</returns>
        private async Task<string> ProcessWebRequestAsync(HttpWebRequest webRequest)
        {
            string responseValue = null;

            if (webRequest != null)
            {
                if (_httpMethod != HttpVerb.GET)
                {
                    // Write request content into web request stream
                    try
                    {
                        using (Stream requestStream = await webRequest.GetRequestStreamAsync())
                        {
                            byte[] contentInBytes = Encoding.UTF8.GetBytes(_content);
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
                    if (_timeoutInMs != null && _timeoutInMs > -1)
                    {
                        abortWebRequestTaskCancellationTokenSource = new CancellationTokenSource();
                        AbortWebRequestOnTimeoutAsync(webRequest, abortWebRequestTaskCancellationTokenSource.Token);
                    }

                    // Make the web request
                    webResponse = (HttpWebResponse)await webRequest.GetResponseAsync();

                    // Cancel the task for aborting web request now as it made the full cycle before the timeout timer got hit
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
                    // Handle different web exceptions
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                        if (statusCode != HttpStatusCode.OK)
                        {
                            // Throw http status code
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

                webResponse?.Dispose();
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
                    await Task.Delay(_timeoutInMs ?? 0); // Set timeout timer
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
