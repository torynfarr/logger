# Client
This application is intended for use in Unity projects. The client script sends http POST requests (specifically serialized JSON data) either directly to the server application or to the optional middleware layer.

Please see the [middleware](https://github.com/torynfarr/logger/tree/master/middleware) section of this repository for further details.

There are two types of messages the client script is designed to send. The first is a setup message which configures the datagrid in the server app with up to fifteen columns and sets the text color. The second is a regular message containing a row of data to be added to the datagrid.

Please see the [server](https://github.com/torynfarr/logger/tree/master/server) section of this repository for further details.

The client is not intended to be built/compiled. Instead, the *Client.cs* file is meant to be added to a GameObject in a scene in Unity.
<br />
<br />

## Usage

### Installation and Configuration
1) Add both the *Client.cs* and *Rest.cs* files to your Unity project.

2) Using [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) or the NuGet package manager of your choice, install the *Newtonsoft.Json* package.

3) Attach *Client.cs* to a new empty GameObject or to an existing GameObject.

4) In the properties of the GameObject you attached the *Client.cs* script to, enter the IP address of the PC on which the server application or the middleware app is running.

5) Enter the TCP port number which the server or middleware app is listening on. By default, the server application is configured to listen on TCP port 4444.

6) If using a new GameObject, add a tag titled *Logger* to the GameObject.

<img src="https://github.com/torynfarr/logger/blob/master/docs/images/client-gameobject-configuration.png" width="800">

### Sending a Setup Message

Choose a script in your Unity project which will send the server application a setup message. Ideally, you'll want this event to occur at the start of your application. In the script you've chosen, add the following using directives:

```c#
using Aporia.Logger.Client;
using Newtonsoft.Json.Linq;
```

Add a public property to the class in your script with the *GameObject* reference type. This will be the game-object in your hierarchy which you attach the *Client* script to.

```c#
public GameObject Logger
```

Add a private property to the class in your script with the *Client* reference type.

```c#
private Client client;
```

In the *Start* or *Awake* class methods, you'll need to set *client* to the *Client* script component of the GameObject it's attached to. For example:

```c#
if (Logger.activeInHierarchy)
{
    client = GameObject.FindGameObjectWithTag("Logger").GetComponent<Client>();
}
```

Where appropriate, use the following code to send the server app the setup message:

```c#
if (Logger.activeInHierarchy)
{
    JObject jObject = new JObject
    {
      new JProperty("MsgType", "Setup"),
      new JProperty("Columns", new List<string> { "Col1", "Col2", "Col3", "Col4", "Col5" }),
      new JProperty("TextColor", "#00ff00")
    };

    Task.Run(async () =>
    {
        await client.PostAsync(jObject);
    });
}
```

By using the *enabled* value as a condition, you'll be able to easily disable the logger in your Unity project without having to modify your code. To do so, simply uncheck the *enabled* checkmark from the GameObject in the scene hierarchy in Unity.

This setup message provides the server with the names of up to fifteen columns in the order in which they should appear in the datagrid and the Hex color value for the text in the datagrid. Replace *Col1*, *Col2*, *Col3*, etc. etc. with whatever column names you'd like to use and replace *#00ff00* with whatever Hex color you'd like the text to be.
<br />
<br />
- The column names *Time* and *ID* should not be used, as the server application automatically creates those columns for you.

- Each column name must be unique.

- Avoid using special characters other than spaces and hyphens.
<br />

### Sending a Regular Message
Whenever an event occurs in your Unity project which you would like to log, you can use the client script to send a message to the server application.
<br />

#### Method 1: Using a JObject
Similar to how the setup message was sent, you can create a JObject to send as a message to the server application. In the JObject, you'll add a key/value pair for each column you've configured via the setup message.

```c#
if (Logger.activeInHierarchy)
{
    JObject jObject = new JObject
    {
        new JProperty("MsgType", "Message"),
        new JProperty("Col1", "Value goes here"),
        new JProperty("Col2", "Value goes here"),
        new JProperty("Col3", "Value goes here"),
        new JProperty("Col4", "Value goes here"),
        new JProperty("Col5", "Value goes here")
    };

    Task.Run(async () =>
    {
        await client.PostAsync(jObject);
    });
}
```
<br />

- Each key in the JSON data should match the name of the corresponding column exactly (case sensitive).

- The value in each key/value pair should be a string data type.

- The order in which each key/value pair is listed in the JSON data is irrelevant.

- You can omit keys for any number of columns to which you don't want to add data in this message. 

- Again, do not include keys named *Time* or *ID* as the server application automatically populates those for you.

#### Method 2: Using a Custom Class

If you'd prefer, you can create a custom C# class with string properties for each of the columns you've configured via the setup message and use this instead of creating a JObject. The downside to this approach, however, is that you can't use spaces in the property names.

```c#
public class Message 
{ 
    public Message()
    {
      MsgType = "Message";
    }

    public string MsgType {get; set; }
    public string Col1 { get; set; }
    public string Col2 { get; set; }
    public string Col3 { get; set; }
    public string Col4 { get; set; }
    public string Col5 { get; set; }
}
```

With your custom class created, you could instantiate an instance of it, serialize and parse it into a JObject, and then send it to the server application. This second approach is likely not as performant as the first approach, however it may make things easier if your custom class performs some extended logic to set the property values. Also, you can set some property values to defaults (such as setting *MsgType* to *Message*) via the class constructor.

```c#
if (Logger.activeInHierarchy)
{
    Message message = new Message
    {
        Col1 = "Value goes here",
        Col2 = "Value goes here",
        Col3 = "Value goes here",
        Col4 = "Value goes here",
        Col5 = "Value goes here"
    };

    Task.Run(async () =>
    {
        await client.PostAsync(JObject.Parse(JsonConvert.SerializeObject(message)));
    });
}
```

## Roadmap
- Testing for network connectivity and disabling the GameObject the script is attached to if no connectivity is detected.
- Support for SSL encryption (sending messages via https).
- If Unity adds support for dynamic types, the *PostAsync* method could be modified making to make it more elegant to call.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. This entire suite is just a tool to assist developers (including myself) working on AR/VR projects. As such, it's not my primary focus. If new features aren't being added, please feel free to fork.
<br />
<br />

## License
This project (including the client, middleware, and server) are [licensed](https://github.com/torynfarr/logger/blob/master/LICENSE) under the GNU General Public License v3.0.
<br />
<br />

## Additional Information

- The application is intended to be used as scripts in a Unity project.
- There is a dependency on the *Newtonsoft.Json* package.
- The application requires network connectivity.
- The middleware application is required if the client script is running on the same computer as the server application.