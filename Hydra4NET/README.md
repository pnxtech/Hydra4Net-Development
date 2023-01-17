# Hydra4NET

![logo](https://user-images.githubusercontent.com/387129/210153567-ffd66c5a-6e15-438a-a1a7-46a7f3ae1b0a.png)

Hydra for .NET is a library module for building Dotnet microservices.

This is an experimental release of an implementation of Hydra for .NET. Years ago (~7 now) Hydra for NodeJS was built to leverage the power of Redis for building microservices. The early Hydra (announced at EmpireNode 2016) offered features such as service discovery, distributed messaging, message load balancing, logging, presence, and health monitoring.

## Caution and Housekeeping
Hydra4Net is under active development and existing functionality may be dramatically changed (read: improved). Updates are offered for testing and feedback purposes. Please use with caution (i.e. not in production) and report any issues you may find.

More about Hydra and related open source projects can be found at: https://github.com/pnxtech where other implementations of Hydra for NodeJS, Python and Dart can be found.

For an example of Hydra4Net messaging in action see the [MessageDemo example](https://github.com/pnxtech/Hydra4Net-Development/tree/master/MessageDemo)

Nuget packaged versions of Hydra4Net are available at: [https://www.nuget.org/packages/Hydra4NET](https://www.nuget.org/packages/Hydra4NET)

## Introduction
Hydra4Net seeks to support the following features:

- Extreme ease of use. Just add it to your project and immedidately start benefiting from microservice functionality.
- Enable your service to be discovered by other services. There's no need to hard code service locations. Additionally, your service can discover other hydra-based services.
  - Using service discovery your service can determine the IP address and port of other services. This is useful for making HTTP (API) requests to other services.
- Enable your service to send and receive messages to other services. 
    - Messages can be sent to a specific service or to a service group.
    - Messages can be queued for a service group.
- Hydra4Net is built to be compatible with other Hydra implementations. This means that you can use Hydra4Net to communicate with a Hydra service written in NodeJS or Python or Dart.

The following features are offered but not required for use. In production cases these features may be handled by cloud container orchestration services such as Kubernetes or Docker Swarm.

- Enable your service to be monitored for health and presence. This is useful for load balancing and service discovery.

### A few words about messaging
Hydra4Net, as well as any other Hydra implementation, depends on the use of JSON-based messages.  More specifically, JSON messages need to be packed in a format known as UMF - [Universal Messaging Format](https://github.com/pnxtech/umf/blob/master/umf.md).  The reason for this requirement is because UMF has support for messaging routing and queuing - where as plain JSON messages do not. Hydra4Net specifically uses the `short form` UMF.

Hydra4Net has built-in support for UMF via its UMF and UMFBase classes.

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

- Replace the `hydra.serviceName` with the name of your service. 
- `hydra.serviceIP` is optional and will be auto-detected at run-time by Hydra4Net. 
- `hydra.servicePort` is the port your service will listen on. Note, that if your service doesn't offer an API or listen on a port then you can set this to 0.
- `hydra.serviceType` is the type of service you are running. This field is largely for descriptive purposes when reviewing via the optional [HydraRouter](https://github.com/pnxtech/hydra-router) API and messaging gateway or while debugging entries in Redis. This field may be blank.
- `hydra.serviceDescription` is a description of your service. This field is largely for descriptive purposes for cases similar to those of `hydra.serviceType`.  This field may be blank.
- You can ignore the `hydra.plugins` branch for now. This is for future use.
- Update the `hydra.redis` branch with the `host`, `port` and `db` of the Redis server you will use for Hydra4Net. Note the hostname can be an IP address or DNS name.  Also you can use a mask pattern to allow Hydra4Net to select from a range of IPs.  To use that specify a pattern such as "10.0.0.*" to restrict IP selection.  Note, this is useful when containerizing your microservice.

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
await hydra.InitAsync(config);
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
        await hydraTests.SendMessageAsync();
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
Receiving messages is handled by the `OnMessageHandler` delegate shown above. Sending messages is handled by the `SendMessageAsync` method. The following example shows how to send a message to a specific service.

```csharp
  PingMsg pingMessage = new();
  pingMessage.To = "hmr-service:/";
  pingMessage.Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/";
  pingMessage.Typ = "ping";
  string json = pingMessage.Serialize();
  await _hydra.SendMessageAsync(pingMessage.To, json);
```

Note that we prepare a message object (more about that later) and we serialize it to JSON. Then we call the `hydra.SendMessageAsync` member with a string containing the route to a service followed by the JSON stringified class object. In the case above that's an instance of the `PingMsg` class.

## Message queues
Hydra4Net supports message queues. This is useful for posting messages to a service that may be busy or not be running at the time the message is sent.

Queue retreival is done for you by this library, and received messages are handled by the `OnQueueMessageReceived` method you implemented earlier.   This is implemented using the QueueProcessor base class within Hydra4NET.  However, you can manually retrieve queue messages if desired.

Message queue handling is done via the `QueueMessageAsync`, `GetQueueMessageAsync` and `MarkQueueMessageAsync` methods.

The following example shows how to use the queueing features of Hydra4Net.

```csharp
  // Create and queue message

  IUMF<QueueMsg> pingMessage = _hydra.CreateUMF("testrig-service:/", "job", new QueueMsg
  {
    JobID = "1234";
    JobType = "Sample Job";
    JobData = "Test Data";
  })

  //ALTERNATIVE
  // UMF<QueueMsg> queueMessage = new();
  // queueMessage.To = "testrig-svcs:/";
  // queueMessage.Frm = _hydra.GetServiceFrom();
  // queueMessage.Typ = "job";
  // queueMessage.Bdy = new QueueMsg
  // {
  //   JobID = "1234";
  //   JobType = "Sample Job";
  //   JobData = "Test Data";
  // }

  await _hydra.QueueMessageAsync(queueMessage.Serialize());

  // Retrieve queued message manually (dequeue)
  string json = await _hydra.GetQueueMessage("testrig-svcs");
  UMF umf = UMF.Deserialize(json);
  if(qm.Typ == "job") {
      UMF<QueueMsg> castedMsg = umf.Cast<QueueMsg>();
      Console.WriteLine(qm?.Bdy.JobID);
      await _hydra.MarkQueueMessageAsync(json, true);
  }
```            
## Retreving Responses

If you need to retrieve a response from another Hydra service, you can use one of the following methods:
- `GetUMFResponseAsync`
- `GetUMFResponseAsync<T>`
- `GetUMFResponseStreamAsync`
- `GetUMFResponseStreamAsync<T>`

`GetUMFResponseAsync` sends a message and returns a `Task` which will resolve when the first message with an `rmid` (and optionally specified `typ`) matching the sent message's `mid` is received.  If you know what body format to expect, you can use `GetUMFResponse<T>` and it will handle casting the body for you.

```csharp
///Sender
var msg = _hydra.CreateUMF<SharedMessageBody>("queuer-svcs:/", "respond", new());
IInboundMessage<SharedMessageBody> resp = await _hydra.GetUMFResponseAsync<SharedMessageBody>(msg, "response");

//Receiver
IUMF<SharedMessageBody> request ; //received request from Sender
IUMF<SharedMessageBody> response = hydra.CreateUMFResponse(request, "response", new SharedMessageBody());
await hydra.SendMessageAsync(sharedMessage);
```

Similarly, if you would like to receive more than one response for a given message, you can use `GetUMFResponseStreamAsync`.  This will send a message and return an `IInboundMessageStream` which will allow you to enumrate the responses (where `rmid` matches the sent message `mid`) via an `IAsyncEnumerable`.  This class will continue to listen for messages until you `Dispose()` it, so it is important to do so once you know that you no longer need to recieve messages (how you determine this is up to you).  If all messages are expected to have the same body format, then you can use the `GetUMFResponseStreamAsync<T>` method and it will handle casting the body for you.

```csharp
///Sender
var msg = _hydra.CreateUMF<SharedMessageBody>("queuer-svcs:/", "response-stream", new());
using (IInboundMessageStream<SharedMessageBody> resp = await _hydra.GetUMFResponseStreamAsync<SharedMessageBody>(msg))
{
    await foreach (var rMsg in resp.EnumerateMessagesAsync())
    {
        //umfs are cast for you 
        IUMF<SharedMessageBody>? rUmf = rMsg?.ReceivedUMF;
        if (rMsg?.Type == "response-stream") 
        {
            //do something
        }
        else if (rMsg?.Type == "response-stream-complete")
        {
            break;
        }
    }
}

//Receiver
IUMF<SharedMessageBody> request ; //received request from Sender
for (var i = 0; i < 5; i++)
{
  IUMF<SharedMessageBody> sharedMessage = hydra.CreateUMFResponse(sm!, "response-stream", new SharedMessageBody());
  await hydra.SendMessageAsync(sharedMessage);
}
IUMF<SharedMessageBody> completeMsg = hydra.CreateUMFResponse(sm!, "response-stream-complete", new SharedMessageBody());
await hydra.SendMessageAsync(completeMsg);
```


