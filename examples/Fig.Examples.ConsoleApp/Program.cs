﻿using Fig.Client.ExtensionMethods;
using Fig.Examples.ConsoleApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddConsole();
});

var configuration = new ConfigurationBuilder()
    .AddFig<ConsoleSettings>(o =>
    {
        o.ClientName = "ConsoleApp";
        o.ClientSecretOverride = "be633c90474448c382c47045b2e172d5xx";
        o.LoggerFactory = loggerFactory;
    }).Build();

var serviceCollection = new ServiceCollection();
serviceCollection.Configure<ConsoleSettings>(configuration);

var serviceProvider = serviceCollection.BuildServiceProvider();

var settings = serviceProvider.GetRequiredService<IOptionsMonitor<ConsoleSettings>>();

Console.WriteLine(settings.CurrentValue.ServiceUsername);

Console.ReadKey();