using Microsoft.EntityFrameworkCore;

namespace TreeApp.DataLayer;

public class ApplicationDbContext
(
    DbContextOptions<ApplicationDbContext> options
) : DbContext(options)
{
    public DbSet<Node> Nodes => Set<Node>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Node>()
            .HasMany(n => n.Children)
            .WithOne(n => n.Parent)
            .HasForeignKey(n => n.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
