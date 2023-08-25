using System.Collections.Generic;
using System.Linq;

namespace Fig.Contracts.ImportExport
{
    public class SettingClientExportDataContract
    {
        public SettingClientExportDataContract(string name, string description, string clientSecret, string? instance, List<SettingExportDataContract> settings, List<PluginVerificationExportDataContract> pluginVerifications, List<DynamicVerificationExportDataContract> dynamicVerifications)
        {
            Name = name;
            Description = description;
            ClientSecret = clientSecret;
            Instance = instance;
            Settings = settings.OrderBy(a => a.Name).ToList();
            PluginVerifications = pluginVerifications.OrderBy(a => a.Name).ToList();
            DynamicVerifications = dynamicVerifications.OrderBy(a => a.Name).ToList();
        }

        public string Name { get; set; }
        
        public string Description { get; }

        public string ClientSecret { get; }

        public string? Instance { get; }

        public List<SettingExportDataContract> Settings { get; }

        public List<PluginVerificationExportDataContract> PluginVerifications { get; }

        public List<DynamicVerificationExportDataContract> DynamicVerifications { get; }
    }
}