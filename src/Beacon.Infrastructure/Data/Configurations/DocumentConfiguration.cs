using Beacon.Domain.Applications;
using Beacon.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Beacon.Infrastructure.Data.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.Content).IsRequired();
        builder.Property(d => d.Error).HasMaxLength(2000);
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(d => d.JobApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.JobApplicationId);
    }
}