using IotApis.Model;
using IotCommon;
using Microsoft.EntityFrameworkCore;

namespace IotApis.Data
{
    public class WeatherContext : DbContext
    {
        public WeatherContext(DbContextOptions<WeatherContext> options) : base(options)
        {
        }

        public DbSet<DeviceTelemetry> DeviceTelemetry { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceTelemetry>()
                .HasNoDiscriminator()
                .ToContainer(Constants.WeatherDataContainer)
                .HasPartitionKey(x => x.deviceId)
                .HasKey(x => x.id);
        }
    }
}
