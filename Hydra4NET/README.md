# Hydra4NET

![logo](Hydra4Net-icon.png)

Hydra for .NET is a library module for building Dotnet microservices.

This is an experimental release of an implementation of Hydra for .NET. Years ago (~7 now) Hydra for NodeJS was built to leverage the power of Redis for building microservices. The early Hydra (announced at EmpireNode 2016) offered features such as service discovery, distributed messaging, message load balancing, logging, presence, and health monitoring.

## Caution and Housekeeping
Hydra4Net is under active development and existing functionality may be dramatically changed (read: improved). Updates are offered for testing and feedback purposes. Please use with caution (i.e. not in production) and report any issues you may find.

More about Hydra and related open source projects can be found at: https://github.com/pnxtech where other implementations of Hydra for NodeJS, Python and Dart can be found.

Nuget packaged versions of Hydra4Net are available at: [https://www.nuget.org/packages/Hydra4NET](https://www.nuget.org/packages/Hydra4NET)

## Introduction
Hydra4Net seeks to support the following features:

- Extreme ease of use. Just add it to your project and immedidately start benefiting from microservice functionality.
- Enable your service to be discovered by other services. There's no need to hard code service locations. Additionally, your service can discover other hydra-based services.
  - Using service discovery your service can determine the IP address and port of other services. This is useful for making HTTP (API) requests to other services.
- Enable your service to send and receive messages to other services. Messages can be sent to a specific service or to a service group.
- Hydra4Net is built to be compatible with other Hydra implementations. This means that you can use Hydra4Net to communicate with a Hydra service written in NodeJS or Python.

The following features are offered but not required for use. In production cases these features may be handled by cloud container orchestration services such as Kubernetes or Docker Swarm.

- Enable your service to be monitored for health and presence. This is useful for load balancing and service discovery.

The following additional features are planned:

- Messaging queues. This will allow your service to receive messages for later retrieval due to being busy or not running.

## The basics

1. Add Hydra4Net to your project, as a dependency reference. Grag the latest Nuget package from: [Hydra4NET on Nuget](https://www.nuget.org/packages/Hydra4NET).
2. Create a JSON configuration file for your service.

```json
{
  "hydra": {
    "serviceName": "testrig-svcs",
    "serviceIP": "",
    "servicePort": 12018,
    "serviceType": "test",
    "serviceDescription": "Dotnet-based experimental service",
    "plugins": {
      "hydraLogger": {
        "logToConsole": true,
        "onlyLogLocally": false
      }
    },
    "redis": {
      "host": "redis",
      "port": 6379,
      "db": 0,
      "options": "abortConnect=false,connectRetry=3,connectTimeout=5000"
    }
  }
}
```

Replace the following values above:

- replace the `hydra.serviceName` with the name of your service. 
- `hydra.serviceIP` is optional and will be auto-detected at run-time by Hydra4Net. 
- `hydra.servicePort` is the port your service will listen on. Note, that if your service doesn't offer an API or listen on a port then you can set this to 0.
- `hydra.serviceType` is the type of service you are running. This field is largely for descriptive purposes when reviewing via the optional [HydraRouter](https://github.com/pnxtech/hydra-router) API and messaging gateway or while debugging entries in Redis. This field may be blank.
- `hydra.serviceDescription` is a description of your service. This field is largely for descriptive purposes for cases similar to those of `hydra.serviceType`.  This field may be blank.
- You can ignore the `hydra.plugins` branch for now. This is for future use.
- Update the `hydra.redis` branch with the `host`, `port` and `db` of the Redis server you will use for Hydra4Net. Note the hostname can be an IP address or DNS name.

3. Ensure that your app is async for Hydra4Net to keep running. In the `testrig` project this is done by using `Microsoft.Extensions.Hosting` as shown here:

```csharp
using Microsoft.Extensions.Hosting;
using Hydra4NET;

// Create an instance of Hydra4Net
Hydra hydra = new();

// Create a Host instance to prevent this console app
// from closing and to track application close
using IHost host = Host.CreateDefaultBuilder(args).Build();
AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
void AppDomain_ProcessExit(object? sender, EventArgs e)
{
    hydra.Shutdown();
}
```

Note, this allows for the detection of the process exit event that helps ensure that the `hydra.Shutdown()` call allows Hydra4Net to clean up after itself.

4. Load the configuration file and start Hydra4Net.

```csharp
// Load the hydra config.json file
//
HydraConfigObject? config = Configuration.Load("config.json");
if (config == null)
{
    Console.WriteLine("Hydra config.json not found");
    Environment.Exit(1);
}

// Initialize Hydra using the loaded config file
await hydra.Init(config);
```

5. Use Hydra4Net to send and receive messages.
The example below shows that a delegate will be called when a message is received. The delegate is passed the message type and the message body. The message body is a string and must be parsed by the application. 

```csharp
// Setup an OnMessageHandler to recieve incoming UMF messages
//
hydra.OnMessageHandler(async (string type, string? message) =>
{
    Console.WriteLine($"{type}: {message}");
    if (type == "testMsg")
    {
        TestMsg? tm = hydraTests.ParseTestMsg(message ?? "");
        Console.WriteLine($"msg: {tm?.Bdy?.Msg}, id: {tm?.Bdy?.Id}");
        await hydraTests.SendMessage();
    }
    else if (type == "ping")
    {
        PingMsg? pm = hydraTests.ParsePingMsg(message ?? "");
        Console.WriteLine($"message: {pm?.Bdy?.Message}");
    }
    await Task.Delay(1);
});
```

## Message sending and receiving
Receiving messages is handled by the `OnMessageHandler` delegate shown above. Sending messages is handled by the `SendMessage` method. The following example shows how to send a message to a specific service.

```csharp
  PingMsg pingMessage = new();
  pingMessage.To = "hmr-service:/";
  pingMessage.Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/";
  pingMessage.Typ = "ping";
  string json = pingMessage.Serialize();
  await _hydra.SendMessage(pingMessage.To, json);
```

Note that we prepare a message object (more about that later) and we serialize it to JSON. Then we call the `hydra.SendMessage` member with a string containing the route to a service followed by the JSON stringified class object. In the case above that's an instance of the `PingMsg` class.


