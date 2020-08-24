using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace logger_middleware
{
    public class Rest
    {
        /// <summary>
        /// The http address which will receive the http request. Example: http://127.0.0.1:4444/
        /// </summary>
        public Uri Host { get; set; }

        /// <summary>
        /// Optional user ID to include in the header of the http request. Basic authentication is supported.
        /// </summary>
        public string UserID { get; set; }

        /// <summary>
        /// Optional password to include in the header of the http request. Basic authentication is supported.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Sends the provided data via an http POST request.
        /// </summary>
        /// <param name="data">The deserialized string data to send.</param>
        /// <returns>The asynchronous task which should be awaited the result of which is the http status code.</returns>
        public async Task<HttpStatusCode> PostAsync(string data)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = Host
            };

            // Configure optional headers for basic authentication (accepts null values).
            byte[] byteArray = Encoding.ASCII.GetBytes(UserID + ":" + Password);
            AuthenticationHeaderValue header = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            client.DefaultRequestHeaders.Authorization = header;

            StringContent content = new StringContent(data, Encoding.UTF8, "text/plain");
            HttpResponseMessage response = await client.PostAsync(Host, content).ConfigureAwait(false);

            client.Dispose();
            return response.StatusCode;
        }
    }
}