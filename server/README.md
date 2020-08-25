<img src="https://github.com/torynfarr/logger/blob/master/docs/images/icon.png" width="88"># Server
This application runs on Windows 10 and receives http POST requests either directly from the client running in a Unity project or via the optional middleware layer. When messages are received, they are displayed in a WinUI3 datagrid.

The primary use-case for this application is to log whatever events you would like as they occur while you're playtesting a game or application created in Unity with more detail and flexibility than the *Debug.Log()* method included in the Unity engine and with the ability to easily export the log.

It's of particular value if you're working on an AR or VR application and you're testing with your code running *on device.* In that scenario, you don't have access to the console in Unity. If you're working on a HoloLens 2 app, because this server application is a UWP app, it can follow you into the experience and allow you to see the messages being logged!

Another potential use-case would be if you have someone remote testing your game or application. The client app in Unity could be configured to point to your WAN IP. Then, if you setup a port forwarding rule in your router, the server application could receive messages over the Internet.
<br />
<br />

## Usage

### Waiting for Setup Message

When launched, the application will list all of the local IPv4 addresses that it's listening on and on which TCP port. By default, the application is configured to listen on TCP port 4444. 

If the server application is running on the same computer as the client, the middleware application needs to be used. The middleware application serves as a workaround for the network isolation and loop back rules which prevent the Logger Server UWP app from receiving requests from the same machine it's running and listening on.

Please see the [middleware](https://github.com/torynfarr/logger/tree/master/middleware) section of this repository for further details.

<img src="https://github.com/torynfarr/logger/blob/master/docs/images/server-awaiting-setup-message.png" width="800">

Before the app will start receiving messages and displaying them in the datagrid, it needs to be sent a setup message from the client. The setup message is JSON data which is serialized and transmitted as an http POST request to the server. 

```json
{
  "MsgType": "Setup",
  "Columns": [
    "Turn",
    "Round",
    "Event",
    "Player",
    "Type",
    "Game-Pieces At Risk",
    "T-Piece at Risk",
    "Moving",
    "Origin",
    "Destination",
    "Capture"
  ],
  "TextColor": "#00ff00"
}
```

This setup message provides the server with the names of up to fifteen columns in the order in which they should appear in the datagrid and the Hex color value for the text in the datagrid.
<br />
<br />
- The column names *Time* and *ID* should not be used, as the server application automatically creates those columns for you.

- Each column name must be unique.

- Avoid using special characters other than spaces and hyphens.
<br />

Please see the [client](https://github.com/torynfarr/logger/tree/master/client) section of this repository for further details.
<br />
<br />

### Receiving Messages

After receiving the setup message, the datagrid will become visible, configured with the columns you specified in the setup message. The server application is now ready to receive messages which will be added to the datagrid in the order in which they are received.

<img src="https://github.com/torynfarr/logger/blob/master/docs/images/server-receiving-messages.png" width="800">

Messages sent by the client application should use the following structure:
```json
{
  "MsgType": "Message",
  "Turn": "1",
  "Round": "1",
  "Event": "Start of Turn",
  "Player": "P1",
  "Type": "HMN",
  "Game-Pieces At Risk": "P1M1, P1M2",
  "T-Piece at Risk": "No"
}
```

- Each key in the JSON data should match the name of the corresponding column exactly (case sensitive).

- The value in each key/value pair should be a string data type.

- The order in which each key/value pair is listed in the JSON data is irrelevant.

- You can omit keys for any number of columns to which you don't want to add data in this message. 

- Again, do not include keys named *Time* or *ID* as the server application automatically populates those for you.

Please see the [client](https://github.com/torynfarr/logger/tree/master/client) section of this repository for further details.
<br />
<br />

### User Interface
The columns in the datagrid can be resized and you can click and drag on a column header to change the order in which they appear. Changing the column order does not impact the order that the key/value pairs in the message need to be in. They can be in any order.

The menu along the left side of the server application provides several useful features.

<img src="https://github.com/torynfarr/logger/blob/master/docs/images/server-menu-buttons.png" width="800">

#### Clear
The clear button lets you clear the datagrid of all rows. **Use Caution!** Once you clear the datagrid, those messages are gone for good.
<br />

#### Export
The export button lets you save the contents of the datagrid as a tab delimited text file. Columns are exported in the order in which they appear in the datagrid.
<br />

#### Restart
The restart button stops and restarts the http listener used by the server application, clears the datagrid of all data and columns, and puts the app back into its initial state where it is waiting to be sent a setup message. As with the *Clear* button, this deletes the messages in the datagrid. Be sure to export them first!
<br />

#### About
The about button displays info about the app, including a link to this GitHub repository.
<br />
<br />

## Roadmap
Some ideas for potential future enhancements include:

- A *Settings* menu option which would include the ability to change the TCP port.
- Move the *About* dialog to a tab in the *Settings*.
- Define a Hex color for selected rows in the setup message.
- Define a Hex color for highlighted rows in the setup message.
- Define a template for row details in the setup message.
- Include optional row details with each message.
- Clickable datagrid rows to view row detail information.
- A dialog prompt to confirm/export data before clearing.
- A dialog prompt to confirm/export data before restarting.
- Loop back exemption to negate the need for the middleware app when receiving messages locally.
- Support for SSL encryption (receiving requests via https)
<br />

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. This entire suite is just a tool to assist developers (including myself) working on AR/VR projects. As such, it's not my primary focus. If new features aren't being added, please feel free to fork.
<br />
<br />

## License
This project (including the client, middleware, and server) are [licensed](https://github.com/torynfarr/logger/blob/master/LICENSE) under the GNU General Public License v3.0.
<br />
<br />

## Additional Information

- This is a UWP application designed for Windows 10 version 2004 (Build 19041).
- The minimum supported version of Windows 10 is version 1903 (Build 18362).
- The application requires network connectivity. At least one local area network adapter in your computer must have a valid IPv4 address.
- The middleware application is required if the server application is running on the same computer as the client which is sending it messages.
- [Transaction List](https://icons8.com/icons/set/transaction-list) icon by [Icon 8](https://icons8.com)