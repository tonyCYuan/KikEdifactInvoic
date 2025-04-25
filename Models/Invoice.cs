namespace KikEdifactInvoic.Models;

public class Invoice
{
    public int Id { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string Description3 { get; set; } = string.Empty;
    public decimal BaseValue { get; set; }
    public decimal BaseVat { get; set; }
    public string Trans { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public int OceanId { get; set; }
    public string OceanSerialNo { get; set; } = string.Empty;
    public string Hbol { get; set; } = string.Empty;
    public string Mbol { get; set; } = string.Empty;
    public List<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
}

public class InvoiceDetail
{
    public int Id { get; set; }
    public int ReferenceId { get; set; }
    public decimal VatRate { get; set; }
    public string ChargeCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaseValue { get; set; }
    public decimal BaseVat { get; set; }
    public string VatCode { get; set; } = string.Empty;
    public string ContainerNo { get; set; } = string.Empty;
    public string ContainerSize { get; set; } = string.Empty;
    public decimal GrossWeight { get; set; }
    public decimal Volume { get; set; }
    public int Quantity { get; set; }
}