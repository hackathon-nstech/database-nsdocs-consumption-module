namespace NSdocs.Application.Documents.DTOs;

public record DocumentListDto
{
    public long Id { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
