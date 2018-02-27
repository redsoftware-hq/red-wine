using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Red.Console
{
    class TestContext : DbContext
    {
        public DbSet<TestModel> TestModels { get; set; }
    }
}
