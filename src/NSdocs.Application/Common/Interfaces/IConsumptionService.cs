using NSdocs.Domain.Enums;

namespace NSdocs.Application.Common.Interfaces;

public interface IConsumptionService
{
    Task UpdateConsumptionAsync(
        int companyId, 
        int quantity, 
        DateTime consumptionDate,
        DocumentOrigin origin,
        DocumentType documentType,
        DocumentStatus status,
        CancellationToken cancellationToken);
}
