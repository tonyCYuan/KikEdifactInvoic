namespace KikEdifactInvoic.Models;

public class ContainerSizeMapping
{
    public string KsmartContainerSize { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string KikContainerSize { get; set; } = string.Empty;
}

public class ContainerSizeMappingData
{
    public List<ContainerSizeMapping> ContainerSizes { get; set; } = new List<ContainerSizeMapping>();
}