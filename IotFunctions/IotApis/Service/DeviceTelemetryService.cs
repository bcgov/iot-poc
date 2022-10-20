using IotApis.Data;
using IotApis.Model;
using IotCommon;
using Microsoft.EntityFrameworkCore;

namespace IotApis.Service
{
    public interface IDeviceTelemetryService
    {
        public Task<List<DeviceTelemetry>> GetDeviceTelemetries(string deviceId, long dateFrom, long dateTo);
    }

    public class DeviceTelemetryService : IDeviceTelemetryService
    {
        private IDbContextFactory<WeatherContext> _contextFactory;

        public DeviceTelemetryService(IDbContextFactory<WeatherContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        public async Task<List<DeviceTelemetry>> GetDeviceTelemetries(string deviceId, long dateFrom, long dateTo)
        {
            using var dbContext = await _contextFactory.CreateDbContextAsync();

            var telemetries = await dbContext.DeviceTelemetry
                .AsNoTracking()
                .Where(x => x.deviceId == deviceId && x.timestamp >= dateFrom && x.timestamp <= dateTo)
                .ToListAsync();

            //convert measurement timestamp from UTC to PST
            foreach(var telemetry in telemetries)
            {
                telemetry.measurements["timestamp"] = DateUtils.ConvertUtcTotalMillisecondsToPst(telemetry.timestamp).ToString("yyyy-MM-dd hh:mm:ss");
            }

            return telemetries;
        }
    }
}
