using KikEdifactInvoic.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.IO;
using System.Text.Json;

namespace KikEdifactInvoic.Repositories;

public class PostgreSqlRepository : IPostgreSqlRepository
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlRepository> _logger;
    private readonly string _invoiceQuery;

    public PostgreSqlRepository(IConfiguration configuration, ILogger<PostgreSqlRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL") ?? 
            throw new ArgumentNullException("PostgreSQL connection string is missing in configuration");
        _logger = logger;
        
        // Load SQL query from the JSON file
        var queryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Queries", "InvoiceQuery.json");
        if (File.Exists(queryFilePath))
        {
            var jsonContent = File.ReadAllText(queryFilePath);
            var queryObject = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            _invoiceQuery = queryObject.GetProperty("InvoiceQuery").GetString() ?? string.Empty;
        }
        else
        {
            _logger.LogWarning("Query file not found: {QueryFilePath}", queryFilePath);
            _invoiceQuery = string.Empty;
        }
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        _logger.LogInformation("Getting invoice data for invoice number: {InvoiceNumber}", invoiceNumber);
        
        // Get the raw query results
        var queryResults = await GetInvoiceQueryResultsAsync(invoiceNumber);
        
        if (queryResults == null || !queryResults.Any())
        {
            _logger.LogWarning("No data found for invoice: {InvoiceNumber}", invoiceNumber);
            return null;
        }
        
        // Group results by invoice and map to Invoice model
        var firstResult = queryResults.First();
        
        var invoice = new Invoice
        {
            Id = firstResult.Id,
            SerialNo = firstResult.SerialNo,
            TransactionType = firstResult.TransactionType,
            TransactionNumber = firstResult.TransactionNumber,
            TransactionDate = firstResult.TransactionDate,
            Description3 = firstResult.Description3,
            BaseValue = firstResult.BaseValue,
            BaseVat = firstResult.BaseVat,
            Trans = firstResult.Trans,
            ExchangeRate = firstResult.ExchangeRate,
            OceanId = firstResult.OceanId,
            OceanSerialNo = firstResult.OceanSerialNo,
            Hbol = firstResult.Hbol,
            Mbol = firstResult.Mbol,
            Details = new List<InvoiceDetail>()
        };
        
        // Add details
        foreach (var result in queryResults)
        {
            // Skip if DetailId is 0 (likely means there's no detail data)
            if (result.DetailId == 0)
                continue;
                
            invoice.Details.Add(new InvoiceDetail
            {
                Id = result.DetailId,
                ReferenceId = result.ReferenceId,
                VatRate = result.VatRate,
                ChargeCode = result.ChargeCode,
                Description = result.Description,
                BaseValue = result.DetailBaseValue,
                BaseVat = result.DetailBaseVat,
                VatCode = result.VatCode,
                ContainerNo = result.ContainerNo,
                ContainerSize = result.ContainerSize,
                GrossWeight = result.GrossWeight,
                Volume = result.Volume,
                Quantity = result.Quantity
            });
        }
        
        return invoice;
    }
    
    public async Task<List<InvoiceQueryResult>> GetInvoiceQueryResultsAsync(string invoiceNumber)
    {
        _logger.LogInformation("Executing invoice query for invoice number: {InvoiceNumber}", invoiceNumber);
        
        var results = new List<InvoiceQueryResult>();
        
        try
        {
            if (string.IsNullOrEmpty(_invoiceQuery))
            {
                _logger.LogError("Invoice query is not loaded.");
                return results;
            }
            
            // Replace parameter placeholders in the query
            var query = _invoiceQuery.Replace("@InvoiceNumbers", $"'{invoiceNumber}'");
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var result = new InvoiceQueryResult
                {
                    Id = reader["id"] as int? ?? 0,
                    SerialNo = reader["serial_no"] as string ?? string.Empty,
                    TransactionType = reader["transaction_type"] as string ?? string.Empty,
                    TransactionNumber = reader["transaction_number"] as string ?? string.Empty,
                    TransactionDate = reader["transaction_date"] as string ?? string.Empty,
                    Description3 = reader["description_3"] as string ?? string.Empty,
                    BaseValue = reader["base_value"] as decimal? ?? 0m,
                    BaseVat = reader["base_vat"] as decimal? ?? 0m,
                    Trans = reader["trans"] as string ?? string.Empty,
                    ExchangeRate = reader["exrate"] as decimal? ?? 0m,
                    
                    OceanId = reader["ocean_id"] as int? ?? 0,
                    OceanSerialNo = reader["ocean_serial_no"] as string ?? string.Empty,
                    Hbol = reader["h_bol"] as string ?? string.Empty,
                    Mbol = reader["m_bol"] as string ?? string.Empty,
                    
                    DetailId = reader["detail_id"] as int? ?? 0,
                    ReferenceId = reader["reference_id"] as int? ?? 0,
                    VatRate = reader["vat_rate"] as decimal? ?? 0m,
                    ChargeCode = reader["charge_code"] as string ?? string.Empty,
                    Description = reader["description"] as string ?? string.Empty,
                    DetailBaseValue = reader["detail_base_value"] as decimal? ?? 0m,
                    DetailBaseVat = reader["detail_base_vat"] as decimal? ?? 0m,
                    VatCode = reader["vat_code"] as string ?? string.Empty,
                    ContainerNo = reader["container_no"] as string ?? string.Empty,
                    ContainerSize = reader["size"] as string ?? string.Empty,
                    GrossWeight = reader["grs_kgs"] as decimal? ?? 0m,
                    Volume = reader["cbm"] as decimal? ?? 0m,
                    Quantity = Convert.ToInt32(reader["qty"] is DBNull ? 0 : reader["qty"])
                };
                
                results.Add(result);
            }
            
            _logger.LogInformation("Retrieved {Count} invoice detail records for {InvoiceNumber}", 
                results.Count, invoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing invoice query for invoice number: {InvoiceNumber}", invoiceNumber);
        }
        
        return results;
    }
}