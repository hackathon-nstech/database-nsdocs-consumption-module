using Microsoft.EntityFrameworkCore;
using NSdocs.Domain.Entities;

namespace NSdocs.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Document> Documents { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
