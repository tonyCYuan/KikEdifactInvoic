namespace KikEdifactInvoic.Models;

public class ChargeCodeMapping
{
    public int Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string BillingType { get; set; } = string.Empty;
    public string KsmartChargeCode { get; set; } = string.Empty;
    public string KsmartChargeCodeDesc { get; set; } = string.Empty;
    public string EdifactCode { get; set; } = string.Empty;
}