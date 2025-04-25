using KikEdifactInvoic.Models;

namespace KikEdifactInvoic.Services;

public interface IChargeCodeMappingService
{
    Task<List<ChargeCodeMapping>> GetAllChargeMappingsAsync();
    Task<ChargeCodeMapping?> GetChargeCodeMappingByCodeAsync(int code);
    Task<ChargeCodeMapping?> GetChargeCodeMappingByKsmartCodeAsync(string ksmartChargeCode);
}