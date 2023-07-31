﻿using System.Collections.Generic;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public SettingsClientDefinitionDataContract(string name, string description, string? instance, List<SettingDefinitionDataContract> settings, List<SettingPluginVerificationDefinitionDataContract> pluginVerifications, List<SettingDynamicVerificationDefinitionDataContract> dynamicVerifications)
        {
            Name = name;
            Description = description;
            Instance = instance;
            Settings = settings;
            PluginVerifications = pluginVerifications;
            DynamicVerifications = dynamicVerifications;
        }

        public string Name { get; }
        
        public string Description { get; }

        public string? Instance { get; }

        public List<SettingDefinitionDataContract> Settings { get; }

        public List<SettingPluginVerificationDefinitionDataContract> PluginVerifications { get; }

        public List<SettingDynamicVerificationDefinitionDataContract> DynamicVerifications { get; set; }
    }
}