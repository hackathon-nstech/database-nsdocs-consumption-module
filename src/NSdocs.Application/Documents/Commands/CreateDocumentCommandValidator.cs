using FluentValidation;
using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Commands;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("Company ID must be greater than 0");
            
        RuleFor(x => x.AccessKey)
            .NotEmpty()
            .WithMessage("Access key is required");
            // .Length(44)
            // .WithMessage("Access key must be exactly 44 characters");
            
        RuleFor(x => x.Origin)
            .IsInEnum()
            .WithMessage("Invalid document origin. Valid values are: " + string.Join(", ", Enum.GetNames(typeof(DocumentOrigin))));
            
        RuleFor(x => x.DocumentType)
            .IsInEnum()
            .WithMessage("Invalid document type. Valid values are: " + string.Join(", ", Enum.GetNames(typeof(DocumentType))));
            
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid document status. Valid values are: " + string.Join(", ", Enum.GetNames(typeof(DocumentStatus))));
    }
}
