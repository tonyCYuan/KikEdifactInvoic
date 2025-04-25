using KikEdifactInvoic.Models;

namespace KikEdifactInvoic.Repositories;

public interface IPostgreSqlRepository
{
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
    
    Task<List<InvoiceQueryResult>> GetInvoiceQueryResultsAsync(string invoiceNumber);
}