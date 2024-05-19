using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

public class DockerComposeServerHostAdapterOptions : IOptions<DockerComposeServerHostAdapterOptions>
{
    [Required]
    public string DockerProcessFilePath { get; set; } = "docker";

    public string? DockerHost { get; set; }

    DockerComposeServerHostAdapterOptions IOptions<DockerComposeServerHostAdapterOptions>.Value => this;
}