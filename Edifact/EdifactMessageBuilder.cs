using KikEdifactInvoic.Models;
using KikEdifactInvoic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KikEdifactInvoic.Edifact;

public class EdifactMessageBuilder : IEdifactMessageBuilder
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EdifactMessageBuilder> _logger;
    private readonly EdifactSettings _settings;
    private readonly IChargeCodeMappingService _chargeCodeMappingService;
    private readonly IContainerSizeMappingService _containerSizeMappingService;

    public EdifactMessageBuilder(
        IConfiguration configuration, 
        ILogger<EdifactMessageBuilder> logger,
        IChargeCodeMappingService chargeCodeMappingService,
        IContainerSizeMappingService containerSizeMappingService)
    {
        _configuration = configuration;
        _logger = logger;
        _chargeCodeMappingService = chargeCodeMappingService;
        _containerSizeMappingService = containerSizeMappingService;
        _settings = _configuration.GetSection("EdifactSettings").Get<EdifactSettings>() ?? 
            throw new ArgumentNullException("EdifactSettings missing in configuration");
            
        // Update sender ID as requested
        _settings.SenderIdentification = "KYBREC_TEST";
    }

    public async Task<string> Build(Invoice invoice)
    {
        _logger.LogInformation("Building EDIFACT message for invoice: {TransactionNumber}", invoice.TransactionNumber);
        
        // Load all charge code mappings
        var chargeMappings = await _chargeCodeMappingService.GetAllChargeMappingsAsync();
        
        // Use invoice ID for message reference and interchange control reference
        var messageNumber = invoice.Id.ToString();
        var interchangeNumber = invoice.Id.ToString();
        
        // Determine document type code based on transaction type
        var documentTypeCode = invoice.TransactionType == "CN" ? "381" : "331";
        
        // Get the current date for file generation date
        var fileGenerationDate = DateTime.Now.ToString("yyyyMMdd");
        
        // Format the transaction date
        var transactionDate = FormatDate(invoice.TransactionDate);
        
        // This is a simplified placeholder implementation to demonstrate the structure
        // In a real implementation, each segment would be properly formatted according to EDIFACT standards
        var message = new List<string>
        {
            "UNA:+,? '",
            $"UNB+UNOC:2+{_settings.SenderIdentification}+KIK+{DateTime.Now:ddMMYY}:{DateTime.Now:HHmm}+{messageNumber}'",
            $"UNH+{messageNumber}+INVOIC:D:01B:UN'",
            $"BGM+{documentTypeCode}+{invoice.TransactionNumber}+9'",
            $"DTM+137:{DateTime.Now:yyyyMMdd}:102'", // Message date
            $"DTM+3:{transactionDate}:102'", // Invoice date
            $"FTX+INV+++STEUERFREIE LEISTUNGEN SIND STEUERFREIE BEFOERDERUNGSLEISTUNGEN NACH PAR. 4 NR. 3 USTG'",
            $"RFF+ABQ:{invoice.Reference}'", // Reference number (B/L number)
            
            // Seller information
            $"NAD+IV+DEDTM01:ZZZ++{_settings.SenderCompanyName}:{_settings.SenderDepartment}+{_settings.SenderStreet}+{_settings.SenderCity}++{_settings.SenderPostcode}+{_settings.SenderCountry}'",
            $"RFF+VA:{_settings.SenderVatNumber}'", // VAT registration number
            
            // Buyer information
            $"NAD+II+{_settings.SenderIdentification}++{_settings.ReceiverCompanyName}+{_settings.ReceiverStreet}+{_settings.ReceiverCity}++{_settings.ReceiverPostcode}+{_settings.ReceiverCountry}'",
            $"RFF+VA:{_settings.ReceiverVatNumber}'", // VAT registration number
            
            // Currency information
            $"CUX+6:EUR++{FormatDecimal(invoice.ExchangeRate)}'", // Invoice currency
            $"CUX+1:EUR++{FormatDecimal(invoice.ExchangeRate)}'", // Payment currency
        };
        
        // Determine the highest VAT rate to use for the summary
        decimal highestVatRate = 0;
        
        // Process shipment-based charges (no container)
        foreach (var detail in invoice.Details.Where(d => string.IsNullOrEmpty(d.ContainerNo)))
        {
            var chargeMapping = chargeMappings.FirstOrDefault(m => m.ChargeCode == detail.ChargeCode);
            string edifactCode = chargeMapping?.EdifactCode ?? "ZZZ";
            string chargeType = chargeMapping?.ChargeType ?? "C";
            string serviceCategoryCode = chargeMapping?.ServiceCategoryCode ?? "DD";
            string description = detail.Description;
            
            message.Add($"ALC+{chargeType}+:C+++{serviceCategoryCode}:{detail.ChargeCode}:{description}'");
            message.Add($"MOA+23:{FormatDecimal(detail.Amount)}:EUR'");
            
            if (detail.VatRate > 0)
            {
                highestVatRate = Math.Max(highestVatRate, detail.VatRate);
                message.Add($"TAX+7+VAT+++:::{FormatDecimal(detail.VatRate)}'");
                message.Add($"MOA+150:{FormatDecimal(detail.Amount * detail.VatRate / 100)}:EUR'");
            }
        }
        
        // Group container-based charges by container
        var containerGroups = invoice.Details
            .Where(d => !string.IsNullOrEmpty(d.ContainerNo))
            .GroupBy(d => d.ContainerNo)
            .ToList();
        
        int lineNumber = 1;
        decimal taxableAmount = 0;
        decimal nonTaxableAmount = 0;
        
        // Process each container group
        foreach (var containerGroup in containerGroups)
        {
            var container = containerGroup.First();
            var sizeMapping = await _containerSizeMappingService.GetSizeMappingAsync(container.ContainerSize);
            string sizeCode = sizeMapping?.EdifactCode ?? "ZZZ";
            
            message.Add($"LIN+{lineNumber}++{container.ContainerNo}:RC::{sizeCode}'");
            
            // Add weight and volume information if available
            if (container.GrossWeight > 0)
            {
                message.Add($"MEA+WT+G+KGM:{FormatDecimal(container.GrossWeight)}'");
            }
            
            if (container.Volume > 0)
            {
                message.Add($"MEA+VOL+AAW+MTQ:{FormatDecimal(container.Volume)}'");
            }
            
            // Add quantity - use the actual quantity or default to 1 if 0
            int qty = container.Quantity > 0 ? container.Quantity : 1;
            message.Add($"QTY+128:{qty:D06}:PK'");
            
            // Add container information
            string equipmentSizeType = sizeMapping?.EquipmentSizeTypeCode ?? "ZZZ";
            message.Add($"EQD+CN+{container.ContainerNo}+{(sizeMapping?.Size ?? "")}::{equipmentSizeType}++4'");
            
            // Process charges for this container
            foreach (var detail in containerGroup)
            {
                var chargeMapping = chargeMappings.FirstOrDefault(m => m.ChargeCode == detail.ChargeCode);
                string edifactCode = chargeMapping?.EdifactCode ?? "ZZZ";
                string chargeType = chargeMapping?.ChargeType ?? "C";
                string serviceCategoryCode = chargeMapping?.ServiceCategoryCode ?? "SC";
                string description = detail.Description;
                
                message.Add($"ALC+{chargeType}+:C+++{serviceCategoryCode}:{detail.ChargeCode}:{description}'");
                
                if (detail.VatRate > 0)
                {
                    highestVatRate = Math.Max(highestVatRate, detail.VatRate);
                    message.Add($"MOA+23:{FormatDecimal(detail.Amount)}:EUR'");
                    message.Add($"MOA+150:{FormatDecimal(detail.Amount * detail.VatRate / 100)}:EUR'");
                    taxableAmount += detail.Amount;
                }
                else
                {
                    message.Add($"MOA+342:{FormatDecimal(detail.Amount)}:EUR'");
                    nonTaxableAmount += detail.Amount;
                }
            }
            
            lineNumber++;
        }
        
        // Add summary section
        message.Add("UNS+S'");
        
        // Add total amounts
        if (taxableAmount > 0)
        {
            message.Add($"MOA+125:{FormatDecimal(taxableAmount)}:EUR'");
        }
        
        if (nonTaxableAmount > 0)
        {
            message.Add($"MOA+342:{FormatDecimal(nonTaxableAmount)}:EUR'");
        }
        
        // Total invoice amount
        decimal totalAmount = taxableAmount + nonTaxableAmount + (taxableAmount * highestVatRate / 100);
        message.Add($"MOA+388:{FormatDecimal(totalAmount)}:EUR'");
        
        // Add VAT information
        if (highestVatRate > 0)
        {
            message.Add($"TAX+7+VAT+++:::{FormatDecimal(highestVatRate)}'");
            message.Add($"MOA+124:{FormatDecimal(taxableAmount * highestVatRate / 100)}:EUR'");
        }
        
        // Close the message
        message.Add($"UNT+{message.Count - 1}+{messageNumber}'");
        message.Add($"UNZ+1+{messageNumber}'");
        
        return string.Join("\n", message);
    }
    
    private string FormatDate(DateTime date)
    {
        return date.ToString("ddMMyyyy");
    }
    
    private string FormatDecimal(decimal value)
    {
        // Format decimal with comma as decimal separator
        return value.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    }
}

public class EdifactSettings
{
    public string SenderIdentification { get; set; } = string.Empty;
    public string ReceiverIdentification { get; set; } = string.Empty;
    public string SenderCompanyName { get; set; } = string.Empty;
    public string SenderDepartment { get; set; } = string.Empty;
    public string SenderStreet { get; set; } = string.Empty;
    public string SenderCity { get; set; } = string.Empty;
    public string SenderPostcode { get; set; } = string.Empty;
    public string SenderCountry { get; set; } = string.Empty;
    public string SenderVatNumber { get; set; } = string.Empty;
    public string ReceiverCompanyName { get; set; } = string.Empty;
    public string ReceiverStreet { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public string ReceiverPostcode { get; set; } = string.Empty;
    public string ReceiverCountry { get; set; } = string.Empty;
    public string ReceiverVatNumber { get; set; } = string.Empty;
}