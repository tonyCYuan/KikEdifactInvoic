using KikEdifactInvoic.Edifact;
using KikEdifactInvoic.Repositories;
using Microsoft.Extensions.Logging;

namespace KikEdifactInvoic.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IPostgreSqlRepository _repository;
    private readonly IEdifactMessageBuilder _edifactBuilder;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IPostgreSqlRepository repository,
        IEdifactMessageBuilder edifactBuilder,
        ILogger<InvoiceService> logger)
    {
        _repository = repository;
        _edifactBuilder = edifactBuilder;
        _logger = logger;
    }

    public async Task<string> GenerateEdifactMessageAsync(string invoiceNumber)
    {
        _logger.LogInformation("Generating EDIFACT message for invoice: {InvoiceNumber}", invoiceNumber);
        
        // Get invoice data from repository
        var invoice = await _repository.GetInvoiceByNumberAsync(invoiceNumber);
        
        if (invoice == null)
        {
            _logger.LogError("Invoice not found: {InvoiceNumber}", invoiceNumber);
            throw new ArgumentException($"Invoice not found: {invoiceNumber}");
        }
        
        // Build EDIFACT message
        var edifactMessage = await _edifactBuilder.Build(invoice);
        
        _logger.LogInformation("EDIFACT message generated successfully for invoice: {InvoiceNumber}", invoiceNumber);
        
        return edifactMessage;
    }
}