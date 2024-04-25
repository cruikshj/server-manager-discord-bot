using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

public class AppSettingsSetup(
    IServerAddressesFeature serverAddressesFeature) 
    : IConfigureOptions<AppSettings>
{
    public IServerAddressesFeature ServerAddressesFeature { get; } = serverAddressesFeature;

    public void Configure(AppSettings appSettings)
    {
        if (appSettings.HostUri is null)
        {
            var address = ServerAddressesFeature.Addresses.FirstOrDefault();
            if (address is not null)
            {
                appSettings.HostUri = new Uri(address);
            }
        }
    }
}
