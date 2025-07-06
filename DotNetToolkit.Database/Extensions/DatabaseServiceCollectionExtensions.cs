using DotNetToolkit.Database.Abstractions;
using DotNetToolkit.Database.Configuration;
using DotNetToolkit.Database.Internal;
using DotNetToolkit.Database.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetToolkit.Database.Extensions
{
    /// <summary>
    /// Provides extension methods for registering database-related services in the dependency injection container.
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {
        /// <summary>
        /// Adds database services and configuration to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configuration">The application configuration containing the database settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Fix: Use Bind instead of Configure to bind the configuration section to the DatabaseSettings class.
            var databaseSettingsSection = configuration.GetSection("DatabaseSettings");
            services.Configure<DatabaseSettings>(databaseSettingsSection);

            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<IDbContext, DbContext>();
            services.AddTransient(typeof(IDataMapper<>), typeof(ReflectionDataMapper<>));
            return services;
        }
    }
}
