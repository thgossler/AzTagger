// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using Azure.ResourceManager;
using Serilog;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AzTagger.Models;

public class AzureContext
{
    private const string DefaultClientAppIdPublicCloud = "5221c0b8-f9e6-4663-ac3c-5baf539290dc";
    private const string DefaultClientAppIdChina = "d1f3df8d-1202-41ef-9e1d-4d9a5c207884";

    private string _clientAppId;
    private bool _clientAppIdWasSet = false;
    private string _azureEnvironmentName;

    public string Name { get; set; }

    public string AzureEnvironmentName
    {
        get => _azureEnvironmentName;
        set
        {
            _azureEnvironmentName = value;
            if (!_clientAppIdWasSet)
            {
                switch (value)
                {
                    case "AzurePublicCloud":
                        _clientAppId = DefaultClientAppIdPublicCloud;
                        break;
                    case "AzureChina":
                        _clientAppId = DefaultClientAppIdChina;
                        break;
                }
            }
        }
    }

    public string TenantId { get; set; }

    public string ClientAppId
    {
        get => _clientAppId;
        set
        {
            _clientAppId = value;
            _clientAppIdWasSet = true;
        }
    }

    public AzureContext() : this("Default")
    {
    }

    public AzureContext(string name, string azureEnvironmentName = null, string tenantId = "", string clientAppId = DefaultClientAppIdPublicCloud)
    {
        Name = name;
        _clientAppId = clientAppId;
        if (string.IsNullOrWhiteSpace(azureEnvironmentName))
        {
            AzureEnvironmentName = "AzurePublicCloud";
        }
        else
        {
            AzureEnvironmentName = azureEnvironmentName;
            try
            {
                var armEnvTest = ArmEnvironment;
            }
            catch (Exception)
            {
                Log.Error($"Invalid Azure Environment '{azureEnvironmentName}' name specified, using default");
                AzureEnvironmentName = "AzurePublicCloud";
            }
        }
        TenantId = tenantId;
    }

    [JsonIgnore]
    [Browsable(false)]
    public ArmEnvironment ArmEnvironment
    {
        get
        {
            var field = typeof(ArmEnvironment).GetField(AzureEnvironmentName);
            if (field == null)
            {
                throw new ArgumentException("Invalid Azure Environment name specified");
            }
            return (ArmEnvironment)field.GetValue(null);
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
