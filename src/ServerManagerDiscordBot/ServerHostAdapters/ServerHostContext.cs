public class ServerHostContext(string serverName, string adapterName, IDictionary<string, string?>? properties)
{
    public string ServerName { get; } = serverName;
    public string AdapterName { get; } = adapterName;
    public IDictionary<string, string?>? Properties { get; } = properties;
}