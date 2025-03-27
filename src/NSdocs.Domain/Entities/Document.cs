using NSdocs.Domain.Enums;

namespace NSdocs.Domain.Entities;

public class Document
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    public string AccessKey { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DocumentOrigin Origin { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
}
