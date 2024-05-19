using Microsoft.Extensions.Options;

public class ProcessServerHostAdapterOptions : IOptions<ProcessServerHostAdapterOptions>
{
    ProcessServerHostAdapterOptions IOptions<ProcessServerHostAdapterOptions>.Value => this;
}