using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Beacon.Domain.Applications;

namespace Beacon.Application.Abstractions;

public interface IBeaconDbContext
{ 
    DbSet<JobApplication> JobApplications { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}