using AegisIdentity.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisIdentity.DataAccess.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .ValueGeneratedNever();

        builder.Property(a => a.Kind)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(a => a.Actor)
               .HasMaxLength(256);

        builder.Property(a => a.Target)
               .HasMaxLength(256);

        builder.Property(a => a.Message)
               .IsRequired()
               .HasMaxLength(512);

        builder.Property(a => a.OccurredAt)
               .IsRequired();

        builder.HasIndex(a => a.OccurredAt)
               .HasDatabaseName("ix_audit_entries_occurred_at");

        builder.HasIndex(a => a.Kind)
               .HasDatabaseName("ix_audit_entries_kind");
    }
}
