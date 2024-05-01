using System.ComponentModel.DataAnnotations;

public class ProcessServerHostProperties
{
    [Required]
    public string FileName { get; set; } = default!;

    public string? Arguments { get; set; }

    public string? WorkingDirectory { get; set; }
}