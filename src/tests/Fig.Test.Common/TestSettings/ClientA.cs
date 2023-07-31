using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientA : SettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "ClientA";


    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
    
    [Setting("This is the address of a website", "http://www.google.com")]
    public string AnotherAddress { get; set; }
}