using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IotApis.HttpClients
{
    public interface IIotCentralApi
    {
        Task<HttpResponseMessage> GetWeatherTelemetry(string deviceId, string dateFrom, string dateTo, string authorization);
        Task<HttpResponseMessage> GeLatesttWeatherTelemetry(string deviceId, string authorization);
        Task<HttpResponseMessage> GetCameraTelemetry(string deviceId, string dateFrom, string dateTo, string authorization);
        Task<HttpResponseMessage> GetCameraLatestTelemetry(string deviceId, string authorization);
        Task<HttpResponseMessage> GetDeviceProperty(string deviceId, string authorization);
    }
    public class IotCentralApi : IIotCentralApi
    {
        private HttpClient _client;
        private IConfiguration _config;
        private ILogger<IotCentralApi> _logger;

        public IotCentralApi(HttpClient client, IConfiguration config, ILogger<IotCentralApi> logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> GetWeatherTelemetry(string deviceId, string dateFrom, string dateTo, string authorization)
        {

            var template = await GetTemplate(deviceId, authorization);

            if (template == "")
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var path = _config.GetValue<string>("IotCentral:TelemetryPath") ?? "";

            var query = $"SELECT $id, $ts, measurements FROM {template}" +
                $" WHERE $ts >= '{dateFrom}' AND $ts <= '{dateTo}' AND $id = '{deviceId}'";

            var body = $"{{ \"query\": \"{query}\" }}";

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            return await _client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> GeLatesttWeatherTelemetry(string deviceId, string authorization)
        {
            var template = await GetTemplate(deviceId, authorization);

            if (template == "")
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var path = _config.GetValue<string>("IotCentral:TelemetryPath") ?? "";

            var query = $"SELECT measurements FROM {template}" +
                $" WHERE WITHIN_WINDOW(P5D) AND $id = '{deviceId}'";

            var body = $"{{ \"query\": \"{query}\" }}";

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            return await _client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> GetCameraTelemetry(string deviceId, string dateFrom, string dateTo, string authorization)
        {
            var template = await GetTemplate(deviceId, authorization);

            if (template == "")
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var path = _config.GetValue<string>("IotCentral:TelemetryPath") ?? "";

            var query = $"SELECT $id, $ts, CameraDatas FROM {template}" +
                $" WHERE $ts >= '{dateFrom}' AND $ts <= '{dateTo}' AND $id = '{deviceId}'";

            var body = $"{{ \"query\": \"{query}\" }}";

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            return await _client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
        }
        public async Task<HttpResponseMessage> GetCameraLatestTelemetry(string deviceId, string authorization)
        {
            var template = await GetTemplate(deviceId, authorization);

            if (template == "")
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var path = _config.GetValue<string>("IotCentral:TelemetryPath") ?? "";

            var query = $"SELECT TOP 1 CameraDatas FROM {template} WHERE $id = '{deviceId}' ORDER BY $ts DESC";

            var body = $"{{ \"query\": \"{query}\" }}";

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            return await _client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> GetDeviceProperty(string deviceId, string authorization)
        {
            var path = string.Format(_config.GetValue<string>("IotCentral:PropertyPath") ?? "", deviceId);

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            return await _client.GetAsync(path);
        }

        public async Task<string> GetTemplate(string deviceId, string authorization)
        {
            var path = string.Format(_config.GetValue<string>("IotCentral:DevicePath") ?? "", deviceId);

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            var responseMessage = await _client.GetAsync(path);

            if (responseMessage.IsSuccessStatusCode)
            {
                var json = await responseMessage.Content.ReadAsStringAsync();
                var jsonObj = JsonConvert.DeserializeObject<dynamic>(json);
                return jsonObj.template;
            }

            return "";
        }
    }
}
