using FluentValidation;

namespace NSdocs.Application.Documents.Commands;

public class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Document ID must be greater than 0");
    }
}
