using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class EncryptionMigrationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallPerformEncryptionMigrationForClients()
    {
        const string settingValue = "test";

        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();

        await SetSettings(settings.ClientName, new List<SettingDataContract>()
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(settingValue))
        });

        Settings.PreviousSecret = Settings.Secret;
        Settings.Secret = "c11210c0fe854bdba85f1119e4d4df9a";

        await PerformMigration();

        Settings.PreviousSecret = string.Empty;

        // It is necessary to log in again because the secret is used to validate the user.
        await ApiClient.Authenticate();
        
        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Count, Is.EqualTo(2));
        var threeSettingsClient = clients.First(a => a.Name == settings.ClientName);
        Assert.That(threeSettingsClient.Settings.Count, Is.EqualTo(3));
        var updatedValue = threeSettingsClient.Settings.First(a => a.Name == nameof(settings.AStringSetting));
        Assert.That(updatedValue.Value?.GetValue(), Is.EqualTo(settingValue));
    }

    // It takes about 10 seconds for each 1000 event logs.
    [Test]
    public async Task ShallPerformEncryptionMigrationForEventLogs()
    {
        var startTime = DateTime.UtcNow;
        const string value1 = "Value1";
        const string value2 = "Value2";
        var settings = await RegisterSettings<ThreeSettings>();

        await SetSettings(settings.ClientName, new List<SettingDataContract>()
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(value1))
        });
        
        await SetSettings(settings.ClientName, new List<SettingDataContract>()
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(value2))
        });

        Settings.PreviousSecret = Settings.Secret;
        Settings.Secret = "c11210c0fe854bdba85f1119e4d4df9a";

        var watch = Stopwatch.StartNew();
        await PerformMigration();
        Console.WriteLine(watch.ElapsedMilliseconds);

        Settings.PreviousSecret = string.Empty;

        // It is necessary to log in again because the secret is used to validate the user.
        await ApiClient.Authenticate();

        var logs = (await GetEvents(startTime, DateTime.UtcNow)).Events.ToList();
        
        Assert.That(logs.Count, Is.EqualTo(4));
        Assert.That(logs.Any(a => a.NewValue == value1));
        Assert.That(logs.Any(a => a.NewValue == value2));
    }

    [Test]
    public async Task ShallPerformEncryptionMigrationForWebHookClients()
    {
        const string secret = "ABCXYZ";
        var clientToCreate = new WebHookClientDataContract(null, 
            "TestClient",
            new Uri("https://localhost:9000"), 
            secret);
        await CreateWebHookClient(clientToCreate);

        Settings.PreviousSecret = Settings.Secret;
        Settings.Secret = "c11210c0fe854bdba85f1119e4d4df9a";

        await PerformMigration();

        Settings.PreviousSecret = string.Empty;

        // It is necessary to log in again because the secret is used to validate the user.
        await ApiClient.Authenticate();
        
        var clients = (await GetAllWebHookClients()).ToList();
        
        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(clients[0].Secret, Is.EqualTo(secret));
    }

    [Test]
    public async Task ShallPerformSettingHistoryMigration()
    {
        const string settingValue = "test";

        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();

        await SetSettings(settings.ClientName, new List<SettingDataContract>()
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(settingValue))
        });

        Settings.PreviousSecret = Settings.Secret;
        Settings.Secret = "c11210c0fe854bdba85f1119e4d4df9a";

        await PerformMigration();

        Settings.PreviousSecret = string.Empty;

        // It is necessary to log in again because the secret is used to validate the user.
        await ApiClient.Authenticate();
        
        var history = (await GetHistory(settings.ClientName, nameof(settings.AStringSetting))).ToList();
        
        Assert.That(history.Count, Is.EqualTo(2));
        Assert.That(history[0].Value, Is.EqualTo(settingValue));
    }
    
    private async Task<HttpResponseMessage> PerformMigration()
    {
        var requestUri = $"/encryptionmigration";

        return await ApiClient.Put<HttpResponseMessage>(requestUri, null);
    }
}