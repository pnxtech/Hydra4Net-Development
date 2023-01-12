# Hydra4NET.HostingExtensions

![logo](https://user-images.githubusercontent.com/387129/210153567-ffd66c5a-6e15-438a-a1a7-46a7f3ae1b0a.png)

Hydra for .NET is a library module for building Dotnet microservices.

This extension library contains helper methods to easily get started with Hydra for ASP.Net core, or any applciation that user Microsoft.Extensions.Hosting.

This is an experimental release of an implementation of Hydra for .NET. Years ago (~7 now) Hydra for NodeJS was built to leverage the power of Redis for building microservices. The early Hydra (announced at EmpireNode 2016) offered features such as service discovery, distributed messaging, message load balancing, logging, presence, and health monitoring.

## Caution and Housekeeping
Hydra4Net is under active development and existing functionality may be dramatically changed (read: improved). Updates are offered for testing and feedback purposes. Please use with caution (i.e. not in production) and report any issues you may find.

More about Hydra and related open source projects can be found at: https://github.com/pnxtech where other implementations of Hydra for NodeJS, Python and Dart can be found.

For an example of Hydra4Net messaging in action see the [HostingMessageDemo example](https://github.com/pnxtech/Hydra4Net-Development/tree/master/HostingMessageDemo)

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

Hydra4Net has built-in support for UMF via its `UMF<TBdy>` classes.

## The basics

1. Add Hydra4Net.HostingExtensions to your project, as a dependency reference. Grag the latest Nuget package from: [Hydra4NET.HostingExtensions on Nuget](https://www.nuget.org/packages/Hydra4NET.HostingExtensions).
2. Add the following section to you appsettings.json or other `ConfigurationProvider`

```json
{
  "HydraConfig" : {
    "Hydra": {
      "ServiceName": "testrig-svcs",
      "ServiceIP": "",
      "ServicePort": 12018,
      "ServiceType": "test",
      "ServiceDescription": "Dotnet-based experimental service",
      "Redis": {
        "Host": "redis",
        "Port": 6379,
        "Db": 0,
        "Options": "abortConnect=false,connectRetry=3,connectTimeout=5000"
      }
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


3. Add the following lines to the `IservicesCollection` configuration.  This will add the necessary services to DI.  Importantly, a singleton `IHydra` instance will be added which can be used throughout the application

```csharp
using Hydra4NET.HostingExtensions;

//This will Load the Hydra configuration from IConfiguration
var config = Configuration.GetHydraConfig();          
services
  .AddHydraServices(config)
  .AddHydraEventHandler<SampleMessageHandler>();
```


4. Make sure to implement the `IHydraEventsHandler` interface and pass the implementation to `AddHydraEventHandler()` above.  You can also use the helper base class `HydraEventsHandler`, which only requires you to implement `OnMessageReceived` and `OnQueueMessageReceived`; you can optionally override the other methods.

5. Use Hydra4Net to send and receive messages.
The example below shows that a delegate will be called when messages are received. The delegate is passed the untyped/serialized message object, the message type, and the message body. The message object can then be cast to the desired concrete type based on the `type` parameter using the `Cast` method.

```csharp
// Setup message handlers to recieve incoming UMF messages
public override async Task OnMessageReceived(UMF umf, string type, string? message, IHydra hydra)
{
    if(type == "type1")
    {
        UMF<SharedMessageBody> castedMsg = umf.Cast<SharedMessageBody>();
        DoSomething(castedMsg);
    }
}
public override async Task OnQueueMessageReceived(UMF umf, string type, string? message, IHydra hydra)
{
    if (type == "type2")
    {
        UMF<CommandMessageBody> castedMsg = umf.Cast<CommandMessageBody>();
        DoSomethingElse(castedMsg);
        // Mark message as processed
        await hydra.MarkQueueMessage(json, true);
    }
}
```

## Message sending and receiving
Receiving messages is handled by the `OnMessageHandler` delegate shown above. Sending messages is handled by the `SendMessage` method. The following example shows how to send a message to a specific service.

```csharp
  UMF<PingMsg> pingMessage = new();
  pingMessage.To = "hmr-service:/";
  pingMessage.Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/";
  pingMessage.Typ = "ping";
  string json = pingMessage.Serialize();
  await _hydra.SendMessage(pingMessage.To, json);
```

Note that we prepare a message object (more about that later) and we serialize it to JSON. Then we call the `hydra.SendMessage` member with a string containing the route to a service followed by the JSON stringified class object. In the case above that's an instance of the `PingMsg` class.

## Message queues
Hydra4Net supports message queues. This is useful for posting messages to a service that may be busy or not be running at the time the message is sent.

Queue retreival is done for you by this library, and received messages are handled by the `OnQueueMessageReceived` method you implemented earlier.   This is implemented using the QueueProcessor base class within Hydra4NET.  However, you can manually retrieve queue messages if desired.

Message queue handling is done via the `QueueMessage`, `GetQueueMessage` and `MarkQueueMessage` members.

The following example shows how to use the queueing features of Hydra4Net.

```csharp
  // Create and queue message
  UMF<QueueMsg> queueMessage = new();
  queueMessage.To = "testrig-svcs:/";
  queueMessage.Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/";
  queueMessage.Typ = "job";
  queueMessage.Bdy.JobID = "1234";
  queueMessage.Bdy.JobType = "Sample Job";
  queueMessage.Bdy.JobData = "Test Data";
  await _hydra.QueueMessage(queueMessage.Serialize());

  // Retrieve queued message manually (dequeue)
  string json = await _hydra.GetQueueMessage("testrig-svcs");
  UMF umf = UMF.Deserialize(json);
  if(qm.Typ == "job") {
      UMF<QueueMsg> castedMsg = umf.Cast<QueueMsg>();
      Console.WriteLine(qm?.Bdy.JobID);
      await _hydra.MarkQueueMessage(json, true);
  }


```            


