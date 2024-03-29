﻿using HostingMessageDemo;
using Hydra4Net.HostingExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((c) =>
    {
        c.AddJsonFile("appsettings.json");
        c.AddEnvironmentVariables();
    })
    .ConfigureServices((cont, services) =>
    {
        var config = cont.Configuration.GetSection("Hydra").GetHydraConfig();
        //(also works due to additional helper) var config = cont.Configuration.GetHydraConfig();
        services
            .AddHydra(config)
            //could also implement IHydraEventsHandler interface without base class
            .AddHydraEventHandler<SampleMessageHandler>();
        services.AddSingleton<Sender>();
        services.AddHostedService<SampleConnectionService>();
    }).Build();

await host.RunAsync();