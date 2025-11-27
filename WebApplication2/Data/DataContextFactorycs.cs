using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rent.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();

            // Wstaw tutaj Twój connection string
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RentDb;Trusted_Connection=True;");

            return new DataContext(optionsBuilder.Options);
        }
    }
}
