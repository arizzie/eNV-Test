using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class EvnContextFactory : IDesignTimeDbContextFactory<EvnContext>
    {
        public EvnContext CreateDbContext(string[] args)
        {

            var optionsBuilder = new DbContextOptionsBuilder<EvnContext>();
            optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("DbConnection"));

            return new EvnContext(optionsBuilder.Options);
        }
    }
}
