using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Beacon.Domain.Applications;
using Beacon.Domain.Documents;

namespace Beacon.Application.Abstractions;

public interface IBeaconDbContext
{ 
    DbSet<JobApplication> JobApplications { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}