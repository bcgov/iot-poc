using IotApis.Model;
using IotCommon;
using Microsoft.EntityFrameworkCore;

namespace IotApis.Data
{
    public class CameraContext : DbContext
    {
        public CameraContext(DbContextOptions<CameraContext> options) : base(options)
        {
        }

        public DbSet<DeviceImage> DeviceImage { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceImage>()
                .HasNoDiscriminator()
                .ToContainer(Constants.CameraDataContainer)
                .HasPartitionKey(x => x.deviceId)
                .HasKey(x => x.id);
        }
    }
}
