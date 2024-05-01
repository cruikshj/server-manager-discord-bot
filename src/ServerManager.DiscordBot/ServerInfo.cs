
public class ServerInfo
{
    public string? HostAdapterName { get; set; }

    public Dictionary<string, string?>? HostProperties { get; set; }

    public string? Game { get; set; }
    
    public string? Icon { get; set; }

    public string? Readme { get; set; }
    
    public string? FilesPath { get; set; }

    public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
}
