﻿using System;
using System.Security;
using Fig.Client.Configuration;
using Fig.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ClientSecret;

public class ClientSecretProvider : IClientSecretProvider
{
    private readonly ILogger<ClientSecretProvider> _logger;
    private readonly IFigOptions _options;
    private SecureString? _clientSecret;

    public ClientSecretProvider(IFigOptions options, ILogger<ClientSecretProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public SecureString GetSecret(string clientName)
    {
        _clientSecret ??= ResolveSecret(clientName);
        return _clientSecret;
    }

    private SecureString ResolveSecret(string clientName)
    {
        if (string.IsNullOrEmpty(clientName))
            throw new FigConfigurationException("Client name must be set");

        _logger.LogInformation($"Resolving client secret from {_options.SecretStore}");
        
        ISecretResolver resolver = _options.SecretStore switch
        {
            SecretStore.AppSettings => new AppSettingsSecretResolver(_options),
            SecretStore.DpApi => new DpApiSettingsSecretResolver(_options),
            SecretStore.EnvironmentVariable => new EnvironmentVariableSecretResolver(clientName),
            SecretStore.InCode => new InCodeSecretResolver(_options, _logger),
            _ => throw new FigConfigurationException($"Unknown secret store {_options.SecretStore}")
        };

        try
        {
            return resolver.ResolveSecret();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve client secret");
            throw;
        }
    }
}