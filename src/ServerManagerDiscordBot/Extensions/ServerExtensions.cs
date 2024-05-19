using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

public static class ServerExtensions
{
    public static string GetDefaultServerAddress(this IServer server)
    {
        return server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
    }
}