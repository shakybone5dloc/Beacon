using Microsoft.EntityFrameworkCore;
using Beacon.Application.Abstractions;
using Beacon.Domain.Applications;

namespace Beacon.Infrastructure.Data;

public class BeaconDbContext(DbContextOptions<BeaconDbContext> options) : DbContext(options), IBeaconDbContext
{
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BeaconDbContext).Assembly);
    }
}