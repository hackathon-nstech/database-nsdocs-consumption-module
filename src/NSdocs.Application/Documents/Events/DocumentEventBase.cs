using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Events;

public abstract class DocumentEventBase
{
    public long DocumentId { get; set; }
    public int CompanyId { get; set; }
    public string AccessKey { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DocumentOrigin Origin { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
}
