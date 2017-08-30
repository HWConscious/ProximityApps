using System;
using System.IO;
using System.Text;
using System.Net;
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
        /// <returns>HTTP response of the request</returns>
        public async Task<string> MakeRequestAsync()
        {
            string responseValue = string.Empty;

            // Create a HttpWebRequest instance for web request
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(EndPoint);
            webRequest.Method = HttpMethod.ToString();
            webRequest.Credentials = Credentials;
            webRequest.Headers = Headers;
            webRequest.ContentType = ContentType;
            
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
                webResponse = (HttpWebResponse)await webRequest.GetResponseAsync();
                
                // Process the response stream (could be JSON, XML or HTML etc.)
                using (Stream webResponseStream = webResponse.GetResponseStream())
                {
                    if (webResponseStream != null)
                    {
                        using (StreamReader reader = new StreamReader(webResponseStream))
                        {
                            // Read the response as string
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
                else
                {
                    throw new Exception("Error in RESTful HTTP request (" + ex.Status.ToString() + "). " + ex.Message);
                }
            }
            if (webResponse != null)
            {
                webResponse.Dispose();
            }

            return responseValue;
        }

        #endregion
    }
}
