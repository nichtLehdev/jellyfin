#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.ArrIntegration.Configuration;

/// <summary>
/// <see cref="IConfigurationFactory" /> implementation for <see cref="ArrIntegrationOptions" />.
/// </summary>
public class ArrIntegrationConfigurationFactory : IConfigurationFactory
{
    /// <inheritdoc />
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        return new[]
        {
            new ConfigurationStore
            {
                ConfigurationType = typeof(ArrIntegrationOptions),
                Key = "arrintegration"
            }
        };
    }
}
