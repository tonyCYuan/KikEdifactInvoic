using KikEdifactInvoic.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KikEdifactInvoic.Services;

public class ChargeCodeMappingService : IChargeCodeMappingService
{
    private readonly ILogger<ChargeCodeMappingService> _logger;
    private List<ChargeCodeMapping>? _chargeMappings;
    private readonly string _mappingFilePath;

    public ChargeCodeMappingService(ILogger<ChargeCodeMappingService> logger)
    {
        _logger = logger;
        _mappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ChargeCodeMappings.json");
    }

    public async Task<List<ChargeCodeMapping>> GetAllChargeMappingsAsync()
    {
        if (_chargeMappings != null)
        {
            return _chargeMappings;
        }

        try
        {
            if (!File.Exists(_mappingFilePath))
            {
                _logger.LogWarning("Charge code mapping file not found: {MappingFilePath}", _mappingFilePath);
                return new List<ChargeCodeMapping>();
            }

            var json = await File.ReadAllTextAsync(_mappingFilePath);
            var mappingData = JsonSerializer.Deserialize<ChargeCodeMappingData>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _chargeMappings = mappingData?.ChargeCodes ?? new List<ChargeCodeMapping>();
            _logger.LogInformation("Loaded {Count} charge code mappings", _chargeMappings.Count);
            return _chargeMappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading charge code mappings");
            return new List<ChargeCodeMapping>();
        }
    }

    public async Task<ChargeCodeMapping?> GetChargeCodeMappingByCodeAsync(int code)
    {
        var mappings = await GetAllChargeMappingsAsync();
        return mappings.FirstOrDefault(m => m.Code == code);
    }

    public async Task<ChargeCodeMapping?> GetChargeCodeMappingByKsmartCodeAsync(string ksmartChargeCode)
    {
        var mappings = await GetAllChargeMappingsAsync();
        return mappings.FirstOrDefault(m => m.KsmartChargeCode.Equals(ksmartChargeCode, StringComparison.OrdinalIgnoreCase));
    }
}

public class ChargeCodeMappingData
{
    public List<ChargeCodeMapping> ChargeCodes { get; set; } = new List<ChargeCodeMapping>();
}