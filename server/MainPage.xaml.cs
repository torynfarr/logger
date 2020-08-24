using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using Windows.UI;
using System.Data.Common;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace logger_server
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        public ICollectionView CollectionView
        {
            get
            {
                return _collectionView;
            }
            set
            {
                Set(ref _collectionView, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ICollectionView _collectionView;
        private HttpListener server;
        private const string port = "4444";
        private bool setup = false;

        public MainPage()
        {
            InitializeComponent();

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                tbStatus.Text = "Unable to start Logger Server.\n\nNetwork connection unavailable.";
                return;
            }

            StartServer();
        }

        /// <summary>
        /// Starts a new http listener, listening on the all local IP addresses currently available.
        /// </summary>
        private void StartServer()
        {
            List<string> addresses = GetLocalIPAddresses();
            server = new HttpListener();
            server.Prefixes.Add($"http://*:{port}/");
            server.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            server.Start();

            if (addresses.Count == 1)
            {
                tbStatus.Text = $"Listening on the following IP address:\t{addresses.First()}:{port}\n\nSend a setup event to configure and start the server.";
            }

            if (addresses.Count > 1)
            {
                string text = string.Empty;

                foreach (string ip in addresses)
                {
                    if (ip == addresses.First())
                    {
                        text = $"Listening on the following IP addresses:\t{ip}:{port}";
                    }
                    else
                    {
                        text = $"{text}\n\n\t\t\t\t\t{ip}:{port}";
                    }
                }

                tbStatus.Text = $"{text}\n\n\nSend a setup message to configure the server.";
            }

            if (addresses.Count == 0)
            {
                tbStatus.Text = "No local area network adapters with an IPv4 address were detected!";
            }
            else
            {
                Listen();
            }
        }

        /// <summary>
        /// Persistently listens for and handles http requests.
        /// </summary>
        private async void Listen()
        {
            while (server.IsListening)
            {
                try
                {
                    HttpListenerContext context = await server.GetContextAsync();
                    await Task.Factory.StartNew(() => ProcessRequestAsync(context));
                }
                catch (ObjectDisposedException)
                {
                    // This exception will occur when the server has been stopped and the listen loop is forced to exit.
                }

            }
        }

        /// <summary>
        /// Handles any http POST request received by the server. Looks for setup requests if the datagrid has not yet been configured. Otherwise, looks for message requests to add to the datagrid.
        /// </summary>
        /// <param name="context">The information received by the http listener.</param>
        /// <returns></returns>
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            Thread.Sleep(1000);

            HttpListenerResponse response = context.Response;

            if (context.Request.HttpMethod == "POST")
            {
                string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();

                if (!string.IsNullOrEmpty(data))
                {
                    // If the server is not currently setup, listen only for message with setup instructions.
                    if (!setup)
                    {
                        // Run task in the UI thread in order to update the UI.
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            JsonDocument message = JsonDocument.Parse(data);

                            if (message.RootElement.TryGetProperty("MsgType", out var value))
                            {
                                if (value.ToString() == "Setup")
                                {
                                    setup = ProcessSetupRequest(message);
                                }
                            }
                        }
                        ).AsTask();
                    }
                    else
                    {
                        // The server is setup and ready to receive standard messages which will be displayed in the datagrid.
                        // Run task in the UI thread in order to update the UI.
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            JsonDocument message = JsonDocument.Parse(data);

                            if (message.RootElement.TryGetProperty("MsgType", out var value))
                            {
                                if (value.ToString() == "Message")
                                {
                                    ProcessMessageRequest(message);
                                }
                            }
                        }
                        ).AsTask();
                    }

                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    // The POST method did not contain any data.
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }

            context.Response.Close();
        }

        /// <summary>
        /// Iterates through all IP addresses and builds a list of local / internal addresses.
        /// </summary>
        /// <returns>A list of local IP addresses saved as strings.</returns>
        private static List<string> GetLocalIPAddresses()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            List<string> local = new List<string>();

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    local.Add(ip.ToString());
                }
            }

            return local;
        }

        /// <summary>
        /// Uses the contents of the setup message to configure the datagrid columns and text color.
        /// </summary>
        /// <param name="json">The JSON data which was received by the server.</param>
        /// <returns>True if the datagrid has been successfully configured.</returns>
        private bool ProcessSetupRequest(JsonDocument json)
        {
            List<string> columns = new List<string>();

            if (json.RootElement.TryGetProperty("Columns", out var value))
            {
                columns = JsonSerializer.Deserialize<List<string>>(value.ToString());
            }

            dgDataGrid.Columns.Clear();

            DataGridTextColumn textcolDateTime = new DataGridTextColumn
            {
                Binding = new Binding
                {
                    Mode = BindingMode.TwoWay,
                    Path = new PropertyPath("Time")
                },

                Header = "Time",
                Tag = "Time",
                Width = DataGridLength.Auto
            };

            dgDataGrid.Columns.Add(textcolDateTime);

            DataGridTextColumn textcolID = new DataGridTextColumn
            {
                Binding = new Binding
                {
                    Mode = BindingMode.TwoWay,
                    Path = new PropertyPath("ID")
                },

                Header = "ID",
                Tag = "ID",
                Width = DataGridLength.Auto
            };

            dgDataGrid.Columns.Add(textcolID);

            int i = 1;

            foreach (string column in columns)
            {
                if (i <= 15)
                {
                    DataGridTextColumn textcol = new DataGridTextColumn
                    {
                        Binding = new Binding
                        {
                            Mode = BindingMode.TwoWay,
                            Path = new PropertyPath($"Column{i}")
                        },

                        Header = column,
                        Tag = column,
                        Width = DataGridLength.Auto
                    };

                    dgDataGrid.Columns.Add(textcol);
                    i++;
                }
            }

            if (json.RootElement.TryGetProperty("TextColor", out var color))
            {
                dgDataGrid.Foreground = new SolidColorBrush(HexToColor(color.ToString()));
            }

            tbStatus.Visibility = Visibility.Collapsed;
            dgDataGrid.Visibility = Visibility.Visible;
            nviRestart.IsEnabled = true;

            return true;
        }

        /// <summary>
        /// Processed the message that was received, translates each property in the JSON data into the appropriate property in the Message class, adds it to the ObservableCollection and updates the datagrid.
        /// </summary>
        /// <param name="json">The JSON data which was received by the server.</param>
        private void ProcessMessageRequest(JsonDocument json)
        {
            DateTime now = DateTime.Now;

            Message message = new Message
            {
                Time = $"{AddTrailingZero(now.Hour)}:{AddTrailingZero(now.Minute)}:{AddTrailingZero(now.Second)}",
                ID = Messages.Count + 1
            };

            // Lookup the value in the JSON data for each column header and assign it to the corresponding generic property in the message to be added to the datagrid.
            foreach (DataGridTextColumn column in dgDataGrid.Columns)
            {
                if (json.RootElement.TryGetProperty(column.Header.ToString(), out var value))
                {
                    switch (column.Binding.Path.Path)
                    {
                        case "Column1":
                            {
                                message.Column1 = value.ToString();
                                break;
                            }

                        case "Column2":
                            {
                                message.Column2 = value.ToString();
                                break;
                            }

                        case "Column3":
                            {
                                message.Column3 = value.ToString();
                                break;
                            }

                        case "Column4":
                            {
                                message.Column4 = value.ToString();
                                break;
                            }

                        case "Column5":
                            {
                                message.Column5 = value.ToString();
                                break;
                            }

                        case "Column6":
                            {
                                message.Column6 = value.ToString();
                                break;
                            }

                        case "Column7":
                            {
                                message.Column7 = value.ToString();
                                break;
                            }

                        case "Column8":
                            {
                                message.Column8 = value.ToString();
                                break;
                            }

                        case "Column9":
                            {
                                message.Column9 = value.ToString();
                                break;
                            }

                        case "Column10":
                            {
                                message.Column10 = value.ToString();
                                break;
                            }

                        case "Column11":
                            {
                                message.Column11 = value.ToString();
                                break;
                            }

                        case "Column12":
                            {
                                message.Column12 = value.ToString();
                                break;
                            }

                        case "Column13":
                            {
                                message.Column13 = value.ToString();
                                break;
                            }

                        case "Column14":
                            {
                                message.Column14 = value.ToString();
                                break;
                            }

                        case "Column15":
                            {
                                message.Column15 = value.ToString();
                                break;
                            }

                    }
                }
            }

            Messages.Add(message);

            if (Messages.Count > 0)
            {
                nviClear.IsEnabled = true;
                nviDownload.IsEnabled = true;
            }

            CollectionViewSource collectionViewSource = new CollectionViewSource
            {
                Source = Messages,
                IsSourceGrouped = false
            };

            CollectionView = collectionViewSource.View;
        }

        /// <summary>
        /// Converts a Hex color code to a Windows.UI.Color ARGB value.
        /// </summary>
        /// <param name="hexString">The Hex color code value. c</param>
        /// <returns>A Windows.UI.Color ARGB value.</returns>
        private static Windows.UI.Color HexToColor(string hexString)
        {
            ColorConverter colorConverter = new ColorConverter();
            System.Drawing.Color color = (System.Drawing.Color)colorConverter.ConvertFromString(hexString);
            return Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Appends a trailing zero to the left of an integer if the integer is less than 10. Used to formate date and time values.
        /// </summary>
        /// <param name="value">The integer to append a trailing zero to.</param>
        /// <returns>A string value for the integer which may include a trailing zero.</returns>
        private string AddTrailingZero(int value)
        {
            string str;

            if (value < 10)
            {
                str = $"0{value}";
            }
            else
            {
                str = value.ToString();
            }

            return str;
        }

        /// <summary>
        /// Closes the NavigationView when the application starts.
        /// </summary>
        /// <param name="sender">Object which triggered this event.</param>
        /// <param name="e">Data related to the event which was triggered.</param>
        private void NavigationViewLoaded(object sender, RoutedEventArgs e)
        {
            nvMenu.IsPaneOpen = false;
        }

        /// <summary>
        /// Tapped event handler for the "Clear" NavigationView item.
        /// </summary>
        /// <param name="sender">Object which triggered this event.</param>
        /// <param name="e">Data related to the event which was triggered.</param>
        private void Clear_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Messages.Clear();
            nviClear.IsEnabled = false;
            nviDownload.IsEnabled = false;
        }

        /// <summary>
        /// Tapped event handler for the "Export" NavigationView item.
        /// </summary>
        /// <param name="sender">Object which triggered this event.</param>
        /// <param name="e">Data related to the event which was triggered.</param>
        private async void Export_TappedAsync(object sender, TappedRoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
          
            var fileSavePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedFileName = $"Logger Export {now.Year}-{AddTrailingZero(now.Month)}-{AddTrailingZero(now.Day)}T{AddTrailingZero(now.Hour)}{AddTrailingZero(now.Minute)}{AddTrailingZero(now.Second)}.txt",
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };

            fileSavePicker.FileTypeChoices.Add("Text (Tab delimited)", new List<string>() { ".txt" });

            StorageFile file = await fileSavePicker.PickSaveFileAsync();

            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);

                List<DataGridTextColumn> columnsInOrder = new List<DataGridTextColumn>();
                List<string> lines = new List<string>();
                string columns = string.Empty;
                string row = string.Empty;

                // Build a list of the columns sorted in the order in which they are currently displayed.
                for (int i = 0; i <= dgDataGrid.Columns.Count - 1; i++)
                { 
                    foreach (DataGridTextColumn column in dgDataGrid.Columns)
                    {
                        if (column.DisplayIndex == i)
                        {
                            columnsInOrder.Add(column);
                        }
                    }
                }

                // Construct the column header line.
                foreach (DataGridTextColumn column in columnsInOrder)
                {
                    if (column == columnsInOrder.First())
                    {
                        columns = $"{column.Header}";
                    }
                    else
                    {
                        columns = $"{columns}\t{column.Header}";
                    }
                }

                lines.Add(columns);

                // Construct each line to be added to the file.
                foreach (Message message in Messages)
                {
                    // Lookup each column in the datagrid and find the corresponding generic property in the message.
                    // This handles the possibility that not all fifteen properties in the Message class were used in the datagrid.
                    foreach (DataGridTextColumn column in columnsInOrder)
                    {
                        switch (column.Binding.Path.Path)
                        {
                            case "Time":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Time}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Time}";
                                    }
                                    break;
                                }

                            case "ID":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.ID}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.ID}";
                                    }
                                    break;
                                }

                            case "Column1":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column1}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column1}";
                                    }
                                    break;
                                }

                            case "Column2":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column2}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column2}";
                                    }
                                    break;
                                }

                            case "Column3":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column3}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column3}";
                                    }
                                    break;
                                }

                            case "Column4":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column4}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column4}";
                                    }
                                    break;
                                }

                            case "Column5":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column5}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column5}";
                                    }
                                    break;
                                }

                            case "Column6":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column6}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column6}";
                                    }
                                    break;
                                }

                            case "Column7":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column7}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column7}";
                                    }
                                    break;
                                }

                            case "Column8":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column8}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column8}";
                                    }
                                    break;
                                }

                            case "Column9":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column9}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column9}";
                                    }
                                    break;
                                }

                            case "Column10":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column10}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column10}";
                                    }
                                    break;
                                }

                            case "Column11":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column11}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column11}";
                                    }
                                    break;
                                }

                            case "Column12":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column12}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column12}";
                                    }
                                    break;
                                }

                            case "Column13":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column13}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column13}";
                                    }
                                    break;
                                }

                            case "Column14":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column14}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column14}";
                                    }
                                    break;
                                }

                            case "Column15":
                                {
                                    if (column == columnsInOrder.First())
                                    {
                                        row = $"{message.Column15}";
                                    }
                                    else
                                    {
                                        row = $"{row}\t{message.Column15}";
                                    }
                                    break;
                                }
                        }
                    }

                    lines.Add(row);
                    row = string.Empty;
                }

                // Write the contents of the datagrid to the file.
                await FileIO.WriteLinesAsync(file, lines);

                // Let the OS know that we're finished changing the file.
                await CachedFileManager.CompleteUpdatesAsync(file);
            }

        }

        /// <summary>
        /// Tapped event handler for the "Restart" NavigationView item.
        /// </summary>
        /// <param name="sender">Object which triggered this event.</param>
        /// <param name="e">Data related to the event which was triggered.</param>
        private void Restart_Tapped(object sender, TappedRoutedEventArgs e)
        {
            server.Abort();
            Messages.Clear();
            setup = false;
            dgDataGrid.Visibility = Visibility.Collapsed;
            dgDataGrid.Columns.Clear();
            nviClear.IsEnabled = false;
            nviDownload.IsEnabled = false;
            nviRestart.IsEnabled = false;
            tbStatus.Visibility = Visibility.Visible;
            StartServer();
        }

        /// <summary>
        /// Tapped event handler for the "About" NavigationView item.
        /// </summary>
        /// <param name="sender">Object which triggered this event.</param>
        /// <param name="e">Data related to the event which was triggered.</param>
        private async void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentDialog aboutDialog = new ContentDialogAbout();
            await aboutDialog.ShowAsync();
        }

        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
