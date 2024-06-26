public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddDirectory(this IConfigurationBuilder builder, string directory)
    {
        if (!Directory.Exists(directory))
        {
            return builder;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var extension = Path.GetExtension(file);
            switch (extension)
            {
                case ".json":
                    builder.AddJsonFile(file, optional: true);
                    break;
                case ".yml":
                case ".yaml":
                    builder.AddYamlFile(file, optional: true);
                    break;
            }
        }

        return builder;
    }
}