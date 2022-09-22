using Microsoft.Extensions.DependencyInjection;

// Define our namespace
namespace SyncStream.Validator.Hostname;

/// <summary>
/// This class maintains the the IServiceCollection extensions
/// </summary>
public static class HostnameValidatorServiceCollectionExtensions
{
    /// <summary>
    /// This method adds the HostnameValidatorServiceDatabaseUpdateWorker to the services collection
    /// </summary>
    /// <param name="instance">The instance of IServiceCollection</param>
    /// <returns><paramref name="instance" /> for a fluid interface</returns>
    public static IServiceCollection UseSyncStreamHostnameValidator(this IServiceCollection instance)
    {
        // Register our background worker
        instance.AddHostedService<HostnameValidatorServiceDatabaseUpdateWorker>();

        // We're done, return the instance
        return instance;
    }
}
