using NSdocs.Domain.Enums;

namespace NSdocs.Domain.Entities;

public class Consumption
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public DateTime ConsumptionDate { get; set; }
    public DocumentOrigin Origin { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
    public int Quantity { get; set; }
    public int Total { get; set; }
}
