using PL.App.Migrations;

namespace PL.App
{
    public static class CommandHandler
    {
        public static async Task<int?> HandleAsync(WebApplication app, string[] args)
        {
            if (args == null || args.Length == 0) return null;

            var command = args[0].ToLower();

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            switch (command)
            {
                case "migrate":
                    Console.WriteLine("Running Migrations");
                    var migrator = services.GetRequiredService<MigrationCommand>();
                    await migrator.RunMigrationsAsync();
                    return 0;

                case "drop-all":
                    Console.WriteLine("Dropping all database objects");
                    var dropper = services.GetRequiredService<MigrationCommand>();
                    await dropper.RunDropAllAsync();
                    return 0;

                case "seed":
                    Console.WriteLine("Seeding database");
                    // Add your seeding logic here
                    return 0;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    return 1;
            }
        }
    }
}
