using Microsoft.Data.SqlClient;

namespace PL.App.Migrations
{
    public class MigrationCommand
    {
        private readonly string _connectionString;
        private readonly string _scriptsDirectory;

        public MigrationCommand(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("PAWS_DB_Connection")
                ?? throw new InvalidOperationException("No valid connection string found in configuration.");

            // Set output to Newer
            _scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "Migrations");
        }

        public async Task RunMigrationsAsync()
        {
            await ExecuteScriptsAsync(runDropAll: false);
        }

        public async Task RunDropAllAsync()
        {
            await ExecuteScriptsAsync(runDropAll: true);
        }

        private async Task ExecuteScriptsAsync(bool runDropAll)
        {
            if (!Directory.Exists(_scriptsDirectory))
            {
                Console.WriteLine($"Directory not found: {_scriptsDirectory}");
                return;
            }

            var sqlFiles = Directory.GetFiles(_scriptsDirectory, "*.sql").OrderBy(f => f);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var filePath in sqlFiles)
            {
                var fileName = Path.GetFileName(filePath);
                bool isDropScript = fileName.StartsWith("999_DropAll", StringComparison.OrdinalIgnoreCase);

                // Route execution based on the requested command
                if (runDropAll && !isDropScript) continue;
                if (!runDropAll && isDropScript) continue;

                Console.WriteLine($"Executing {(runDropAll ? "drop" : "migration")} script: {fileName}");

                var sql = await File.ReadAllTextAsync(filePath);

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }

            Console.WriteLine("Done.");
        }
    }
}
