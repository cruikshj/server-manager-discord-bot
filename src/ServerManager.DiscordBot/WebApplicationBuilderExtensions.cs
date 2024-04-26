public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureLargeFileDownloadHandler(this WebApplicationBuilder builder)
    {
        var type = builder.Configuration.GetValue<LargeFileDownloadHandlerType>("LargeFileDownloadHandler");

        switch (type)
        {
            case LargeFileDownloadHandlerType.BuiltIn:
                builder.Services.AddTransient<ILargeFileDownloadHandler, BuiltInLargeFileDownloadHandler>();
                break;
        }
        
        return builder;
    }
}