using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace logger_middleware
{
    class Program
    {
        private static HttpListener Receiver = new HttpListener();
        private static string receiverIP;
        private static string receiverPort;
        private static string serverIP;
        private static string serverPort;

        /// <summary>
        /// Listens for http requests and relays them to the Logger Server.
        /// This middleware application serves as a workaround for the network isolation and loop back rules which prevents the Logger Server UWP app from receiving requests from the same machine it's running and listening on.
        /// </summary>
        /// <param name="args">Expected command line arguments include the IP address and port to listen on and the IP address and port to relay messages to.</param>
        /// <returns>An asynchronous operation / task.</returns>
        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

            // Confirm that at least two command line arguments were provided. Display help text if they were not.
            if (string.IsNullOrEmpty(args.ToString()) || args.Length != 2 || args[0].ToLower() == "help")
            {
                Help();
                return;
            }

            string[] receiverPrefix = args[0].Split(":");
            string[] serverPrefix = args[1].Split(":");

            if (receiverPrefix.Length != 2 || serverPrefix.Length != 2)
            {
                Help();
                return;
            }
            
            receiverIP = receiverPrefix[0];
            receiverPort = receiverPrefix[1];
            serverIP = serverPrefix[0];
            serverPort = serverPrefix[1];

            // Configure and start the http listener.
            Receiver.Prefixes.Add($"http://{receiverIP}:{receiverPort}/");
            Receiver.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            Receiver.IgnoreWriteExceptions = true;
            Receiver.Start();

            Console.Clear();
            Console.WriteLine("Logger Middleware");
            Console.WriteLine();
            Console.Write($"Listening on {receiverIP}:{receiverPort}");

            await ListenAsync();
        }

        /// <summary>
        /// Cleans up the http listener on exit.
        /// </summary>
        /// <param name="sender">The event handler object which raised the event.</param>
        /// <param name="args">The cancel key that was pressed which triggered the event.</param>
        protected static void CancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            Receiver.Close();
            Receiver = null;
            Console.WriteLine();
            return;
        }

        /// <summary>
        /// Outputs help text explaining what command line arguments to enter when launching this application.
        /// </summary>
        static void Help()
        {
            Console.WriteLine();
            Console.WriteLine("Logger Middleware");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run [ip address to listen on]:[port to listen on] [ip address to relay to]:[port to relay to]");
            Console.WriteLine();
            Console.WriteLine("Example: dotnet run 172.22.179.31:4444 192.168.83.42:4444");
            Console.WriteLine();
        }

        /// <summary>
        /// Handle data sent to the http listener. When a POST request is received, relay it to the IP address and port specified via the command line arguments.
        /// </summary>
        /// <returns>The asynchronous task which should be awaited.</returns>
        static async Task ListenAsync()
        {
            // Continuously listen for Http POST requests.
            while (Receiver.IsListening)
            {
                await Task.Delay(500);

                HttpListenerContext context = await Receiver.GetContextAsync();
                HttpListenerResponse response = context.Response;

                if (context.Request.HttpMethod == "POST")
                {
                    string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();

                    if (!string.IsNullOrEmpty(data))
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine($"Message received: \n\n{data}");
                        Console.WriteLine();
                        response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.Close();
                        await RelayMessageAsync(data);

                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine($"Null or empty message received!");
                        Console.WriteLine();
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.Close();
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"A request other than POST was received!");
                    Console.WriteLine();
                }

                context.Response.Close();

                Console.Write($"Listening on {receiverIP}:{receiverPort}");

            }
        }

        /// <summary>
        /// Relays POST requests to the IP address and port specified via the command line arguments.
        /// </summary>
        /// <param name="data">The serialized string data to relay.</param>
        /// <returns>The asynchronous task which should be awaited.</returns>
        static async Task RelayMessageAsync(string data)
        {
            Rest client = new Rest
            {
                Host = new Uri($"http://{serverIP}:{serverPort}")
            };

            Console.WriteLine($"Relaying message to {client.Host}");
            Console.WriteLine();

            try
            {
                HttpStatusCode response = await client.PostAsync(data).ConfigureAwait(false);

                if (response == HttpStatusCode.OK)
                {
                    Console.WriteLine($"Message relayed!");
                    Console.WriteLine();
                }
                else // If needed, handling of other responses can be added here.
                {
                    Console.WriteLine($"Error: The Logger server responded with status code {response}");
                    Console.WriteLine();
                }
            }
            // Handle a potential connection time out if the receiver did not respond.
            catch (AggregateException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine();
            }
        }
    }
}
