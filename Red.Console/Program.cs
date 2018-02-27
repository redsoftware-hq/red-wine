using Red.Wine;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Red.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            TestContext context = new TestContext();
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<TestContext, Migrations.Configuration>());
            context.Database.Initialize(true);
            WineRepository<TestModel> repository = new WineRepository<TestModel>(context, "Jarvis");

            var model = repository.CreateNewWineModel(new TestOptions { Name = "Test Man" });
        }
    }
}
