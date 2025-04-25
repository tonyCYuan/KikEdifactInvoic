using KikEdifactInvoic.Models;

namespace KikEdifactInvoic.Edifact;

public interface IEdifactMessageBuilder
{
    Task<string> Build(Invoice invoice);
}