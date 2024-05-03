using System.ComponentModel.DataAnnotations;

public class DockerComposeServerHostProperties
{
    [Required]
    public string DockerComposeFilePath { get; set; } = default!;
}