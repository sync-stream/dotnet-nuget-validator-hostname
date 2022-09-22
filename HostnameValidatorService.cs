using System.Text.Json.Serialization;
using System.Xml.Serialization;

// Define our namespace
namespace SyncStream.Validator.Hostname;

/// <summary>
/// This class maintains the service provider structure for the PublicSuffix hostname validator
/// </summary>
[XmlRoot("validatedHostname")]
public class HostnameValidatorService
{
    /// <summary>
    /// This constant defines the URL to the Public Suffix database
    /// </summary>
    private const string PublicSuffixDatabaseUrl = "https://publicsuffix.org/list/public_suffix_list.dat";

    /// <summary>
    /// This property contains the Public Suffix database
    /// </summary>
    private static readonly List<string> PublicSuffixDatabase = new();

    /// <summary>
    /// This delegate defines a refresh callback
    /// </summary>
    /// <param name="processedLine">The line that was processed</param>
    /// <param name="sourceLine">The raw line <paramref name="processedLine" /> was generated from</param>
    public delegate void DelegateRefreshCallback(string processedLine, string sourceLine);

    /// <summary>
    /// This delegate defines an asynchronous refresh callback
    /// </summary>
    /// <param name="processedLine">The line that was processed</param>
    /// <param name="sourceLine">The raw line <paramref name="processedLine" /> was generated from</param>
    /// <returns>An awaitable task containing a void result</returns>
    public delegate System.Threading.Tasks.Task DelegateRefreshCallbackAsync(string processedLine, string sourceLine);

    /// <summary>
    /// This property contains the domain parsed from the source
    /// </summary>
    [JsonPropertyName("domain")]
    [XmlAttribute("domain")]
    public string Domain { get; set; }

    /// <summary>
    /// This property contains the host parsed from the source
    /// </summary>
    [JsonPropertyName("host")]
    [XmlAttribute("host")]
    public string Host { get; set; }

    /// <summary>
    /// This property contains the valid flag for the hostname
    /// </summary>
    [JsonPropertyName("valid")]
    [XmlAttribute("valid")]
    public bool IsValid { get; set; }

    /// <summary>
    /// This property contains the port parsed from the source if there was one
    /// </summary>
    [JsonPropertyName("port")]
    [XmlAttribute("port")]
    public int? Port { get; set; }

    /// <summary>
    /// This property contains the protocol parsed from the source if there was one
    /// </summary>
    [JsonPropertyName("protocol")]
    [XmlAttribute("protocol")]
    public string Protocol { get; set; }

    /// <summary>
    /// This property contains the source to that was parsed
    /// </summary>
    [JsonPropertyName("source")]
    [XmlAttribute("source")]
    public string Source { get; set; }

    /// <summary>
    /// This property contains the TLD parsed from the source
    /// </summary>
    [JsonPropertyName("tld")]
    [XmlAttribute("tld")]
    public string TopLevelDomain { get; set; }

    /// <summary>
    /// This method instantiates the class
    /// </summary>
    public HostnameValidatorService()
    {
    }

    /// <summary>
    /// This method instantiates the class with a source URI to parse
    /// </summary>
    /// <param name="source">The source URI</param>
    public HostnameValidatorService(Uri source) => Parse(source);

    /// <summary>
    /// This method instantiates the class with a source string to parse
    /// </summary>
    /// <param name="source">The source string</param>
    public HostnameValidatorService(string source) => Parse(source);

    /// <summary>
    /// This method determines whether the <paramref name="line" /> is valid for sanitizing or not
    /// </summary>
    /// <param name="line">The line in question</param>
    /// <returns>A boolean denoting validity</returns>
    private static bool IsValidLine(string line) =>
        !string.IsNullOrEmpty(line.Trim()) && !string.IsNullOrWhiteSpace(line.Trim()) &&
        !line.Trim().StartsWith("//") && !line.Trim().StartsWith('#');

    /// <summary>
    /// This method sanitizes a <paramref name="line" /> from the database
    /// </summary>
    /// <param name="line">The line to sanitize</param>
    /// <returns>The sanitized <paramref name="line" /></returns>
    private static string SanitizeLine(string line) =>
        line.Trim().Replace("!.", "").Replace("*.", "").Replace("*", "").Replace("!", "").Trim().ToLower();

    /// <summary>
    /// This method iterates over the Public Suffix Database and stores it in memory with an optional callback
    /// </summary>
    /// <param name="database">The location of the database to query</param>
    /// <param name="callback">The callback to execute with each line</param>
    private static void LocalizePublicSuffixDatabase(string database, DelegateRefreshCallback callback = null)
    {
        // Clear the database
        PublicSuffixDatabase.Clear();
        // Iterate over the lines
        database.Split(Environment.NewLine).ToList().ForEach(line =>
        {
            // Make sure we have a valid line
            if (!IsValidLine(line)) return;

            // Localize the processed line
            string processedLine = SanitizeLine(line);

            // Add the TLD to the instance
            PublicSuffixDatabase.Add(processedLine);

            // Check for a callback
            callback?.Invoke(processedLine, line);
        });
    }

    /// <summary>
    /// This method asynchronously iterates over the Public Suffix Database and stores it in memory with an optional callback
    /// </summary>
    /// <param name="database">The location of the database to query</param>
    /// <param name="callback">The callback to execute with each line</param>
    private static async Task LocalizePublicSuffixDatabaseAsync(string database,
        DelegateRefreshCallbackAsync callback = null)
    {
        // Clear the database
        PublicSuffixDatabase.Clear();

        // Iterate over the lines
        foreach (string line in database.Split(Environment.NewLine))
        {
            // Make sure we have a valid line
            if (!IsValidLine(line)) return;

            // Localize the processed line
            string processedLine = SanitizeLine(line);

            // Add the TLD to the instance
            PublicSuffixDatabase.Add(processedLine);

            // Check for a callback
            if (callback != null) await callback.Invoke(processedLine, line);
        }
    }

    /// <summary>
    /// This method determines the host of the source
    /// </summary>
    /// <param name="source">The source in question</param>
    /// <param name="domain">The domain name to replace</param>
    /// <param name="host">The output host name</param>
    private static void DetermineHost(string source, string domain, out string host)
    {
        // Determine the host
        host = source.Replace(domain, "").TrimEnd('.').ToLower();
    }

    /// <summary>
    /// This method determines the Top Level Domain (TLD) from the source
    /// </summary>
    /// <param name="source">The source in question</param>
    /// <param name="isValid">Output valid denoting validity</param>
    /// <param name="topLevelDomain">Output value of the top-level-domain</param>
    /// <param name="domain">The output value of the domain</param>
    private static void DetermineTopLevelDomain(string source, out bool isValid, out string topLevelDomain,
        out string domain)
    {
        // Ensure we have a database
        if (!PublicSuffixDatabase.Any()) LocalizePublicSuffixDatabase(PublicSuffixDatabaseUrl);

        // Split the domain parts
        List<string> parts = source.Split('.')
            .Where(s => !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        // Define our working TLD
        string workingTld = parts.Last().Trim().ToLower();

        // Remove the last part of the list
        parts.RemoveAt(parts.Count - 1);

        // Define our found flag
        bool found = false;

        // Iterate until we've found it
        while (parts.Any())
        {
            // Localize the TLD
            string tld = PublicSuffixDatabase.FirstOrDefault(s => s.Equals(workingTld));

            // Check to see if we've found our TLD
            if (string.IsNullOrEmpty(tld) || string.IsNullOrWhiteSpace(tld))
            {

                // Check for parts
                if (parts.Any())
                {
                    // Reset the working TLD
                    workingTld = $"{parts.Last().Trim().ToLower()}.{workingTld}";
                    // Remove the last part from the list
                    parts.RemoveAt(parts.Count - 1);
                }
            }
            else
            {

                // Reset the found flag
                found = true;
                // We're done, break the loop
                break;
            }
        }

        // Check the found flag
        if (!found)
        {

            // Set the valid flag
            isValid = false;

            // Set the domain
            domain = null;

            // Set the top level domain
            topLevelDomain = null;

            // We're done
            return;
        }

        // Check for a working TLD
        if (!string.IsNullOrEmpty(workingTld.Trim()) && !string.IsNullOrWhiteSpace(workingTld.Trim()))
        {

            // Set the valid flag in the instance
            isValid = true;

            // Set the top level domain
            topLevelDomain = workingTld.Trim().ToLower();

            // Check for parts and set the domain
            if (parts.Any()) domain = $"{parts.Last().Trim().ToLower()}.{workingTld.Trim().ToLower()}";

            // Otherwise set the domain to the working TLD
            else domain = workingTld.Trim().ToLower();
        }
        else
        {

            // Set the valid flag in the instance
            isValid = false;

            // Set the top level domain
            topLevelDomain = null;

            // Set the domain
            domain = null;
        }
    }

    /// <summary>
    /// This method parses a URI source
    /// </summary>
    /// <param name="source">The source in question</param>
    /// <returns>The parsed <paramref name="source" /></returns>
    public static HostnameValidatorService Parse(Uri source) => Parse(source.Host.ToLower());

    /// <summary>
    /// This method parses a string source
    /// </summary>
    /// <param name="source">The source in question</param>
    /// <returns>The parsed <paramref name="source" /></returns>
    public static HostnameValidatorService Parse(string source)
    {
        // Instantiate our response
        HostnameValidatorService response = new();

        // Check for a port
        if (source.Contains(':'))
        {

            // Set the port into the response
            response.Port = int.Parse(source.Split(':', StringSplitOptions.RemoveEmptyEntries).Last().Trim());

            // Set the source into the response
            response.Source = source.Split(':', StringSplitOptions.RemoveEmptyEntries).First().Trim().ToLower();
        }

        // Otherwise, set the response source to the input source
        else response.Source = source.Trim().ToLower();

        // Determine the top level domain
        DetermineTopLevelDomain(response.Source, out bool isValid, out string tld, out string domain);

        // Set the validation flag into the response
        response.IsValid = isValid;

        // Check the valid response
        if (response.IsValid)
        {

            // Set the top level domain into the response
            response.TopLevelDomain = tld;

            // Set the domain into the response
            response.Domain = domain;

            // Determine the host
            DetermineHost(response.Source, response.Domain, out string host);

            // Set the host into the response
            response.Host = host;
        }

        // We're done, send the response
        return response;
    }

    /// <summary>
    /// This method refreshes the Public Suffix Database with an optional callback
    /// </summary>
    /// <param name="callback">The callback to execute for each line</param>
    public static void RefreshPublicSuffixDatabase(DelegateRefreshCallback callback = null)
    {
        // Localize our HTTP client
        using HttpClient client = new();

        // Download the Public Suffix database
        string publicSuffixDatabase = client.GetStringAsync(new Uri(PublicSuffixDatabaseUrl)).Result;

        // Localize the database into memory
        LocalizePublicSuffixDatabase(publicSuffixDatabase, callback);
    }

    /// <summary>
    /// This method asynchronously refreshes the Public Suffix Database with an optional asynchronous callback
    /// </summary>
    /// <param name="callback">The callback to execute for each line</param>
    /// <returns>An awaitable task containing a void result</returns>
    public static async Task RefreshPublicSuffixDatabaseAsync(
        DelegateRefreshCallbackAsync callback = null)
    {
        // Localize our WebClient
        using HttpClient client = new();

        // Download the Public Suffix database
        string publicSuffixDatabase = await client.GetStringAsync(new Uri(PublicSuffixDatabaseUrl));

        // Localize the database into memory
        await LocalizePublicSuffixDatabaseAsync(publicSuffixDatabase, callback);
    }

    /// <summary>
    /// This method converts the instance to a fully qualified domain name
    /// </summary>
    /// <returns>The fully-qualified domain name</returns>
    public string ToFullyQualifiedDomainName() =>
        string.IsNullOrEmpty(Host) || string.IsNullOrWhiteSpace(Host) ? Domain : $"{Host}.{Domain}";

    /// <summary>
    /// This method generates the wildcard for the domain
    /// </summary>
    /// <returns>The wildcard domain name</returns>
    public string ToWildcardDomain() =>
        $"*.{Domain}";
}
