using KikEdifactInvoic.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KikEdifactInvoic.Services;

public class ContainerSizeMappingService : IContainerSizeMappingService
{
    private readonly ILogger<ContainerSizeMappingService> _logger;
    private List<ContainerSizeMapping>? _containerSizeMappings;
    private readonly string _mappingFilePath;

    public ContainerSizeMappingService(ILogger<ContainerSizeMappingService> logger)
    {
        _logger = logger;
        _mappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ContainerSizeMappings.json");
    }

    public async Task<List<ContainerSizeMapping>> GetAllContainerSizeMappingsAsync()
    {
        if (_containerSizeMappings != null)
        {
            return _containerSizeMappings;
        }

        try
        {
            if (!File.Exists(_mappingFilePath))
            {
                _logger.LogWarning("Container size mapping file not found: {MappingFilePath}", _mappingFilePath);
                return new List<ContainerSizeMapping>();
            }

            var json = await File.ReadAllTextAsync(_mappingFilePath);
            var mappingData = JsonSerializer.Deserialize<ContainerSizeMappingData>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _containerSizeMappings = mappingData?.ContainerSizes ?? new List<ContainerSizeMapping>();
            _logger.LogInformation("Loaded {Count} container size mappings", _containerSizeMappings.Count);
            return _containerSizeMappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading container size mappings");
            return new List<ContainerSizeMapping>();
        }
    }

    public async Task<ContainerSizeMapping?> GetSizeMappingAsync(string ksmartContainerSize)
    {
        var mappings = await GetAllContainerSizeMappingsAsync();
        return mappings.FirstOrDefault(m => m.KsmartContainerSize.Equals(ksmartContainerSize, StringComparison.OrdinalIgnoreCase));
    }
}