using KikEdifactInvoic.Models;

namespace KikEdifactInvoic.Services;

public interface IContainerSizeMappingService
{
    Task<List<ContainerSizeMapping>> GetAllContainerSizeMappingsAsync();
    Task<ContainerSizeMapping?> GetSizeMappingAsync(string ksmartContainerSize);
}