using System.ComponentModel.DataAnnotations;

public class KubernetesServerHostProperties
{
    [Required]
    public string Kind { get; set; } = default!;

    [Required]
    public string Namespace { get; set; } = default!;

    [Required]
    public string Name { get; set; } = default!;
}