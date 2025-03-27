using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Events;

public class DocumentUpdatedEvent : DocumentEventBase
{
    public DocumentOrigin PreviousOrigin { get; set; }
    public DocumentStatus PreviousStatus { get; set; }
    public int PreviousCompanyId { get; set; }
}
