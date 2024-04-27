public static class LargeFileDownloadHandlerConfigurationHelpers
{
    public static readonly string ConfigurationKey = "LargeFileDownloadHandler";

    public static WebApplicationBuilder ConfigureLargeFileDownloadHandler(this WebApplicationBuilder builder)
    {
        var type = builder.Configuration.GetValue<LargeFileDownloadHandlerType>(ConfigurationKey);

        switch (type)
        {
            case LargeFileDownloadHandlerType.BuiltIn:
                builder.Services.AddTransient<ILargeFileDownloadHandler, BuiltInLargeFileDownloadHandler>();
                break;
        }
        
        return builder;
    }
}