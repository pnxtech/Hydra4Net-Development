﻿

using HostingDemo;
using Hydra4Net.HostingExtensions;
using Hydra4NET;
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
            var config = cont.Configuration.GetSection("HydraConfig").GetHydraConfig();
            
            //(also works due to additional helper) var config = cont.Configuration.GetHydraConfig();
            services.AddHydraServices(config)
            //could also implement interface without base class
            .AddHydraEventHandler<SampleMessageHandler>();
        }).Build();

await host.RunAsync();