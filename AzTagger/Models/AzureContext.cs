// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using Azure.ResourceManager;
using System;
using System.Text.Json.Serialization;

namespace AzTagger.Models;

public class AzureContext
{
    public string Name { get; set; }
    public string AzureEnvironmentName { get; set; }
    public string TenantId { get; set; }
    public string ClientAppId { get; set; }

    public AzureContext(string name = "Default", string azureEnvironmentName = null, string tenantId = "", string clientAppId = "5221c0b8-f9e6-4663-ac3c-5baf539290dc")
    {
        Name = name;
        AzureEnvironmentName = azureEnvironmentName;
        if (string.IsNullOrWhiteSpace(azureEnvironmentName))
        {
            AzureEnvironmentName = "AzurePublicCloud";
        }
        else
        {
            AzureEnvironmentName = azureEnvironmentName;
            try
            {
                var armEnvTest = this.ArmEnvironment;
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid Azure Environment name specified");
            }
        }
        TenantId = tenantId;
        ClientAppId = clientAppId;
    }

    [JsonIgnore]
    public ArmEnvironment ArmEnvironment { 
        get {
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
