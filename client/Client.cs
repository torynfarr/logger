using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace logger_client
{
    public class Client : MonoBehaviour
    {
        /// <summary>
        /// The IP address of the PC on which the logging server or middleware is running.
        /// </summary>
        [Tooltip("IP address of the PC on which the logging server is running.")]
        public string IP;

        /// <summary>
        /// The TCP port on which the logging server or middleware is listening.
        /// </summary>
        [Tooltip("TCP port on which the logging server is listening.")]
        public int Port;

        private Rest client;

        /// <summary>
        /// Configures the game object this script is attached to so that it persists as scenes change.
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Instantiates a new rest client configured to send data to the IP address and port specified in the respective properties.
        /// </summary>
        private void Start()
        {
            client = new Rest
            {
                Host = new Uri($"http://{IP}:{Port}/")
            };
        }

        /// <summary>
        /// Sends the provided message via an http POST request.
        /// </summary>
        /// <param name="message">The JObject message to send.</param>
        /// <returns>The asynchronous task which should be awaited.</returns>
        public async Task PostAsync(JObject message)
        {
            // We're using ToString() rather than JsonConvert.SerializeObject() as it makes the text easy to read in the console if using the middleware application.
            string data = message.ToString();
            HttpStatusCode status = await client.PostAsync(data);
            Debug.Log($"Logger sent a message. Response code: {status}");
        }
    }
}


