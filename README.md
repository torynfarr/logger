# Logger
Logger is a suite of three applications:
<br />
<br />

### [Client](https://github.com/torynfarr/logger/tree/master/client)
A client script designed for use in a Unity project sends messages to either the middleware or server app. There are two types of messages the client script is designed to send. The first is a setup message which configures the datagrid in the server app with up to fifteen columns and sets the text color. The second is a regular message containing a row of data to be added to the datagrid.
<br />
<br />

### [Middleware](https://github.com/torynfarr/logger/tree/master/middleware)
An optional middleware console application can be used to relay messages from the client to the server. This serves as a workaround for the network isolation and loop back rules in Windows 10 which prevent the Logger Server (a UWP application) from receiving requests from the same machine it's running and listening on.
<br />
<br />

### [Server](https://github.com/torynfarr/logger/tree/master/server)
A UWP server application which receives messages and displays them in a customizable WinUI3 datagrid.
<br />
<br />

## Use-case
The primary use-case for this suite of applications is to log whatever events you would like as they occur while you're playtesting a game or application created in Unity. It provides more detail and flexibility than the *Debug.Log()* method included in the Unity engine and gives you the ability to easily export the log.

It's of particular value if you're working on an AR or VR application and you're testing with your code running *on device.* In that scenario, you don't have access to the console in Unity. If you're working on a HoloLens 2 app, because the server application is a UWP app, it can follow you into the experience and allow you to see the messages being logged!

Another potential use-case would be if you have someone remote testing your game or application. The client app in Unity could be configured to point to your WAN IP. Then, if you setup a port forwarding rule in your router, the server application could receive messages over the Internet.
<br />
<br />
Here is an example of all three applications in use:

<img src="https://github.com/torynfarr/logger/blob/master/docs/images/client-middleware-server.gif" width="800">

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. This entire suite is just a tool to assist developers (including myself) working on AR/VR projects. As such, it's not my primary focus. If new features aren't being added, please feel free to fork.
<br />
<br />

## License
This project (including the client, middleware, and server) are [licensed](https://github.com/torynfarr/logger/blob/master/LICENSE) under the GNU General Public License v3.0.
<br />
<br />