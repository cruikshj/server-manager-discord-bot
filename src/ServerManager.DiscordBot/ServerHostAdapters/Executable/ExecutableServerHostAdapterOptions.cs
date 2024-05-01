using Microsoft.Extensions.Options;

public class ExecutableServerHostAdapterOptions : IOptions<ExecutableServerHostAdapterOptions>
{
    ExecutableServerHostAdapterOptions IOptions<ExecutableServerHostAdapterOptions>.Value => this;
}