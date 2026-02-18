using Microsoft.EntityFrameworkCore;
using Oscillation.Core.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Abstractions;

public abstract class SignalStoreDbContext : DbContext
{
    public DbSet<Signal> Signals { get; set; }
    
    protected SignalStoreDbContext(DbContextOptions options) : base(options) {}
}

public interface ISignalStoreDbContextFactory
{
    public SignalStoreDbContext Create();
}