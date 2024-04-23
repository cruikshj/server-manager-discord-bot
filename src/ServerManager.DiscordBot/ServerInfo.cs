
public class ServerInfo
{
    public string? Game { get; set; }
    
    public string? Icon { get; set; }

    public string? Readme { get; set; }
    
    public string? BackupsPath { get; set; }

    public string? Deployment { get; set; }

    public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
}
