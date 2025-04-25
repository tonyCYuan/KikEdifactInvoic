using KikEdifactInvoic.Edifact;
using KikEdifactInvoic.Models;
using KikEdifactInvoic.Repositories;
using KikEdifactInvoic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KikEdifactInvoic;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Check for debug mode
        bool debugMode = args.Contains("--debug");
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup dependency injection
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IPostgreSqlRepository, PostgreSqlRepository>()
            .AddSingleton<IEdifactMessageBuilder, EdifactMessageBuilder>()
            .AddSingleton<IInvoiceService, InvoiceService>()
            .AddSingleton<IChargeCodeMappingService, ChargeCodeMappingService>()
            .AddSingleton<IContainerSizeMappingService, ContainerSizeMappingService>()
            .BuildServiceProvider();

        // Get logger and services
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var invoiceService = serviceProvider.GetRequiredService<IInvoiceService>();
        var chargeCodeMappingService = serviceProvider.GetRequiredService<IChargeCodeMappingService>();
        var containerSizeMappingService = serviceProvider.GetRequiredService<IContainerSizeMappingService>();
        var repository = serviceProvider.GetRequiredService<IPostgreSqlRepository>();

        try
        {
            logger.LogInformation("Starting KIK EDIFACT INVOIC D01B message generation");

            // Load charge code mappings
            var chargeMappings = await chargeCodeMappingService.GetAllChargeMappingsAsync();
            logger.LogInformation("Loaded {Count} charge code mappings", chargeMappings.Count);
            
            // Load container size mappings
            var containerSizeMappings = await containerSizeMappingService.GetAllContainerSizeMappingsAsync();
            logger.LogInformation("Loaded {Count} container size mappings", containerSizeMappings.Count);

            // Get invoice numbers from args or use defaults for testing
            var invoiceNumbers = args.Where(a => !a.StartsWith("--")).ToArray();
            if (invoiceNumbers.Length == 0)
            {
                invoiceNumbers = new[] { "BRESIC25040001", "BRESII25040002" };
            }

            foreach (var invoiceNumber in invoiceNumbers)
            {
                logger.LogInformation("Processing invoice: {InvoiceNumber}", invoiceNumber);
                
                // If in debug mode, print detailed invoice information
                if (debugMode)
                {
                    var invoice = await repository.GetInvoiceByNumberAsync(invoiceNumber);
                    if (invoice != null)
                    {
                        Console.WriteLine($"[DEBUG] Invoice: {invoice.TransactionNumber}");
                        Console.WriteLine($"[DEBUG] Details Count: {invoice.Details.Count}");
                        
                        foreach (var detail in invoice.Details)
                        {
                            Console.WriteLine($"[DEBUG] Detail ID: {detail.Id}");
                            Console.WriteLine($"[DEBUG]   - ChargeCode: {detail.ChargeCode}");
                            Console.WriteLine($"[DEBUG]   - Description: {detail.Description}");
                            Console.WriteLine($"[DEBUG]   - Container: {detail.ContainerNo}");
                            Console.WriteLine($"[DEBUG]   - Size: {detail.ContainerSize}");
                            Console.WriteLine($"[DEBUG]   - Quantity: {detail.Quantity}");
                            Console.WriteLine($"[DEBUG]   - GrossWeight: {detail.GrossWeight}");
                            Console.WriteLine($"[DEBUG]   - Volume: {detail.Volume}");
                            Console.WriteLine();
                        }
                    }
                }
                
                // Generate EDIFACT message
                var edifactMessage = await invoiceService.GenerateEdifactMessageAsync(invoiceNumber);
                
                // Save to file
                var fileName = $"INVOICE_KYBREC_{DateTime.Now:yyyyMMddHHmmss}_{invoiceNumber}.edi";
                await File.WriteAllTextAsync(fileName, edifactMessage);
                
                logger.LogInformation("EDIFACT message generated and saved to: {FileName}", fileName);
            }
            
            logger.LogInformation("EDIFACT message generation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during EDIFACT message generation");
        }
    }
}