# Middleware
The middleware application receives http POST requests from the client and relays them to the server application. This serves as a workaround for the network isolation and loop back rules in Windows 10 which prevent the Logger Server (a UWP application) from receiving requests from the same machine it's running and listening on. 

The middleware app is a console application which runs under the .NET Core 3.1 framework. To function properly, it needs to be running on the same local area network as the server application. Whatever it's running on must have an IPv4 address different from the server application. You could run this middleware app on WSL2 via the Windows Terminal, on a dedicated virtual machine, or on a separate computer (a Raspberry Pi, for example). It can run on any operating system on which .NET Core 3.1 is available.
<br />
<br />

## Usage
Using your preferred CLI, navigate to the location where the middleware app has been stored and start the application using the following command:
```dos
dotnet run [ip address to listen on]:[port to listen on] [ip address to relay to]:[port to relay to]
```
Example:
```dos
 dotnet run 172.22.179.31:4444 192.168.83.42:4444
```
You can stop the application at any time by pressing *CTRL C*.

<img src="https://github.com/torynfarr/logger/blob/main/docs/images/middleware-relaying-messages.png" width="800">

## Roadmap
- If loop back exemption can be added to the server application, this middleware app will no longer be needed.
- Support for SSL encryption (sending and receiving requests via https)
<br />

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. This entire suite is just a tool to assist developers (including myself) working on AR/VR projects. As such, it's not my primary focus. If new features aren't being added, please feel free to fork.
<br />
<br />

## License
This project (including the client, middleware, and server) are [licensed](https://github.com/torynfarr/logger/blob/main/LICENSE) under the GNU General Public License v3.0.
<br />
<br />

## Additional Information

- This is a console application designed for the .NET Core 3.1 framework.
