using Microsoft.Extensions.Hosting;

// Define our namespace
namespace SyncStream.Validator.Hostname;

/// <summary>
/// This class maintains the background service worker for updating the HostnameValidatorService's database
/// </summary>
public class HostnameValidatorServiceDatabaseUpdateWorker : BackgroundService
{
    /// <summary>
    /// This method asynchronously updates the HostnameService database from PublicSuffix
    /// </summary>
    /// <param name="stoppingToken">The token that denotes cancellation</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        // Iterate until we get told to stop
        while (!stoppingToken.IsCancellationRequested)
        {

            // Refresh the Public Suffix Database into memory
            await HostnameValidatorService.RefreshPublicSuffixDatabaseAsync();

            // We're done, wait for 24 hours before running again
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
