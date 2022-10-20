using IotApis.Data;
using IotApis.Model;
using IotCommon;
using Microsoft.EntityFrameworkCore;

namespace IotApis.Service
{
    public interface IDeviceImageDataService
    {
        Task<List<DeviceImage>> GetDeviceImageData(string deviceId, long dateFrom, long dateTo, string? preset);
    }
    public class DeviceImageDataService : IDeviceImageDataService
    {
        private IDbContextFactory<CameraContext> _contextFactory;

        public DeviceImageDataService(IDbContextFactory<CameraContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        public async Task<List<DeviceImage>> GetDeviceImageData(string deviceId, long dateFrom, long dateTo, string? preset)
        {
            using var dbContext = await _contextFactory.CreateDbContextAsync();

            var images = await dbContext.DeviceImage
                .AsNoTracking()
                .Where(x => x.deviceId == deviceId && x.TimeStamp >= dateFrom && x.TimeStamp <= dateTo && (preset == null || x.PreSet == preset))
                .ToListAsync();

            //convert SnapshotDate timestamp from UTC to PST
            foreach (var image in images)
            {
                image.SnapshotDate = DateUtils.ConvertUtcTotalMillisecondsToPst(image.TimeStamp).ToString("yyyy-MM-dd hh:mm:ss");
            }
            return images;
        }
    }
}
