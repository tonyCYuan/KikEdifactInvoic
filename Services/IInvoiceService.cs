namespace KikEdifactInvoic.Services;

public interface IInvoiceService
{
    Task<string> GenerateEdifactMessageAsync(string invoiceNumber);
}