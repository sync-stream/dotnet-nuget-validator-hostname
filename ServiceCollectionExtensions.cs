using Microsoft.Extensions.DependencyInjection;

// Define our namespace
namespace SyncStream.Validator.Hostname;

/// <summary>
/// This class maintains the the IServiceCollection extensions
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// This method adds the HostnameValidatorServiceDatabaseUpdateWorker to the services collection
    /// </summary>
    /// <param name="instance">The instance of IServiceCollection</param>
    public static void UseSyncStreamHostnameValidator(this IServiceCollection instance)
    {
        // Register our background worker
        instance.AddHostedService<HostnameValidatorServiceDatabaseUpdateWorker>();
    }
}
