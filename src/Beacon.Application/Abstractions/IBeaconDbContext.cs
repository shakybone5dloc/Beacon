using Microsoft.EntityFrameworkCore;
using Beacon.Domain.Applications;

namespace Beacon.Application.Abstractions;

public interface IBeaconDbContext
{ 
    DbSet<JobApplication> JobApplications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}