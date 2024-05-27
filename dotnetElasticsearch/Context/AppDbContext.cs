using Microsoft.EntityFrameworkCore;

namespace dotnetElasticsearch.Context;

public class AppDbContext:DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=elastic;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
    }

    public DbSet<Travel> Travels { get; set; }
}
public sealed class Travel
{
    public int  Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}
