using Microsoft.EntityFrameworkCore;
using practice.DataContext.Models;

namespace projects.DataContext;

public class PracticeDbContext : DbContext
{
    public PracticeDbContext(DbContextOptions<PracticeDbContext> options) : base(options)
    {
        
    }
    public DbSet<Employee> Employees { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}