using Beacon.Domain.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Beacon.Infrastructure.Data.Configurations;

internal sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Company).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Role).HasMaxLength(200).IsRequired();
        builder.Property(a => a.JobUrl).HasMaxLength(2048);
        builder.Property(a => a.Notes).HasMaxLength(4000);
        builder.Property(a => a.Version).IsRowVersion();
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(a => a.CreatedAt);
    }
}