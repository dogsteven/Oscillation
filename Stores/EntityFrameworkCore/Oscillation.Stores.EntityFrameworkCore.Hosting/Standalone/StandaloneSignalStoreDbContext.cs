using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oscillation.Core.Abstractions;
using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting.Standalone;

public class StandaloneSignalStoreDbContext : SignalStoreDbContext
{
    private readonly string? _schema;
    private readonly string? _prefix;
    
    public StandaloneSignalStoreDbContext(DbContextOptions options, string? schema, string? prefix) : base(options)
    {
        _schema = schema;
        _prefix = prefix;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SignalEntityConfiguration(_schema, _prefix));
    }
}

public class SignalEntityConfiguration : IEntityTypeConfiguration<Signal>
{
    private readonly string? _schema;
    private readonly string? _prefix;

    public SignalEntityConfiguration(string? schema, string? prefix)
    {
        _schema = schema;
        _prefix = prefix;
    }
    
    public void Configure(EntityTypeBuilder<Signal> builder)
    {
        builder.ToTable($"{_prefix ?? ""}Signals", _schema);
        
        builder.HasKey(signal => new { signal.Group, signal.LocalId });
        
        builder.Property(signal => signal.Group)
            .HasColumnName("Group")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(signal => signal.LocalId)
            .HasColumnName("LocalId")
            .IsRequired();

        builder.Property(signal => signal.Payload)
            .HasColumnName("Payload")
            .IsRequired()
            .HasMaxLength(-1);

        builder.Property(signal => signal.FireTime)
            .HasColumnName("FireTime")
            .IsRequired();

        builder.Property(signal => signal.DistributingStatus)
            .HasColumnName("DistributingStatus")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(signal => signal.NextFireTime)
            .HasColumnName("NextFireTime")
            .IsRequired();

        builder.Property(signal => signal.ProcessingTimeout)
            .HasColumnName("ProcessingTimeout")
            .IsRequired(false);

        builder.Property(signal => signal.RetryAttempts)
            .HasColumnName("RetryAttempts")
            .IsRequired();

        builder.Property(signal => signal.FinalizedTime)
            .HasColumnName("FinalizedTime")
            .IsRequired(false);

        builder.Property(signal => signal.DeadTime)
            .HasColumnName("DeadTime")
            .IsRequired(false);
        
        builder.HasIndex(signal => new { signal.DistributingStatus, signal.NextFireTime });

        builder.HasIndex(signal => signal.DeadTime);

        builder.HasIndex(signal => new { signal.DistributingStatus, signal.ProcessingTimeout });
    }
}