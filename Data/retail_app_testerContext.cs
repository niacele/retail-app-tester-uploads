using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using retail_app_tester.Models;

namespace retail_app_tester.Data
{
    public class retail_app_testerContext : DbContext
    {
        public retail_app_testerContext (DbContextOptions<retail_app_testerContext> options)
            : base(options)
        {
        }

        public DbSet<retail_app_tester.Models.Customer> Customer { get; set; } = default!;
        public DbSet<retail_app_tester.Models.Order> Order { get; set; } = default!;
        public DbSet<retail_app_tester.Models.Product> Product { get; set; } = default!;
    }
}
