using Microsoft.Extensions.Configuration;

public static class ConfigurationSetup
{
    public static void Configure(IConfigurationBuilder config)
    {
        // Clear any existing configuration sources
        if (config is IConfigurationBuilder configBuilder)
        {
            configBuilder.Sources.Clear();
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            // Development environment: only use development settings
            config.SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
                 .AddEnvironmentVariables();
        }
        else
        {
            // Production environment: use production settings
            config.SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddEnvironmentVariables();
        }
    }
}

