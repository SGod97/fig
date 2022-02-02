using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class DiscountService : SettingsBase
{
    public override string ClientName => "DiscountService";
    public override string ClientSecret => "5c56e4b1-165b-47cd-97c3-e1c31a35e391";

    [Setting("This is a string", "Horse")]
    public string AStringSetting { get; set; }

    [Setting("This is an int", 6)]
    public int AnIntSetting { get; set; }

    [Setting("This is a bool setting", true)]
    public bool ABoolSetting { get; set; }
}