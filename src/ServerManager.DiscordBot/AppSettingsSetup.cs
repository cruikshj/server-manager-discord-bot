using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

public class AppSettingsSetup(
    IServer server) 
    : IConfigureOptions<AppSettings>
{
    public IServer Server { get; } = server;

    public void Configure(AppSettings appSettings)
    {
        if (appSettings.HostUri is null)
        {
            var serverAddressesFeature = Server.Features.Get<IServerAddressesFeature>();
            var address = serverAddressesFeature?.Addresses.FirstOrDefault() ?? "http://localhost:8080";
            if (address is not null)
            {
                appSettings.HostUri = new Uri(address);
            }
        }
    }
}
