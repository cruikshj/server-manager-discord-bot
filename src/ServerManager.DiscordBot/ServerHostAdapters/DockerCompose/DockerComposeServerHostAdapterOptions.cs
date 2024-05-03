using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

public class DockerComposeServerHostAdapterOptions : IOptions<DockerComposeServerHostAdapterOptions>
{
    [Required]
    public string DockerProcessFilePath { get; set; } = "docker";

    DockerComposeServerHostAdapterOptions IOptions<DockerComposeServerHostAdapterOptions>.Value => this;
}