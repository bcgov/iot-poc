using Azure.Storage.Blobs;
using IotApis.HttpClients;
using IotApis.Model;
using IotApis.Service;
using IotCommon;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.IO.Compression;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace IotApis.Controllers
{
    [ApiController]
    [Route("api/camera/devices")]
    public class DeviceImageDataController : IotControllerBase
    {
        private readonly IDeviceImageDataService _deviceImageService;
        private readonly IIotCentralApi _iotCentralApi;
        private readonly IConfiguration _config;
        private readonly ILogger<DeviceImageDataController> _logger;

        public DeviceImageDataController(IDeviceImageDataService deviceImageService, IIotCentralApi iotCentralApi, IConfiguration config, ILogger<DeviceImageDataController> logger)
        {
            _deviceImageService = deviceImageService;
            _iotCentralApi = iotCentralApi;
            _config = config;
            _logger = logger;
        }

        [HttpGet("{deviceId}/imagedata", Name = "GetDeviceImageData")]
        public async Task<ActionResult<IEnumerable<DeviceImage>>> GetDeviceImageData(string deviceId, string dateFrom, string dateTo, string? preset)
        {
            if (!ValidateDate(dateFrom) || !ValidateDate(dateTo))
                return BadRequest();

            var dateFromTs = DateUtils.ConvertPacificToUtcTotalMilliseconds(DateTime.Parse(dateFrom));
            var dateToTs = DateUtils.ConvertPacificToUtcTotalMilliseconds(DateTime.Parse(dateTo).AddDays(1).AddSeconds(-1));

            return await _deviceImageService.GetDeviceImageData(deviceId, dateFromTs, dateToTs, preset);
        }

        [HttpGet("iotcentral/{deviceId}/imagedata", Name = "GetIotCentralDeviceImageData")]
        public async Task<ActionResult> GetIotCentralDeviceImageData(string deviceId, string dateFrom, string dateTo, [FromHeader] string authorization)
        {
            if (!ValidateDate(dateFrom) || !ValidateDate(dateTo))
                return BadRequest();

            var dateFromTs = DateTime.Parse(dateFrom).ToString("o", CultureInfo.InvariantCulture);
            var dateToTs = DateTime.Parse(dateTo).AddDays(1).AddSeconds(-1).ToString("o", CultureInfo.InvariantCulture);

            var responseMessage = await _iotCentralApi.GetCameraTelemetry(deviceId, dateFromTs, dateToTs, authorization);

            return await HandleResponseMessage(responseMessage);
        }

        [HttpGet("iotcentral/{deviceId}/images/latest", Name = "GetLatestIotCentralDeviceImageData")]
        public async Task<ActionResult> GetLatestIotCentralDeviceImages(string deviceId, [FromHeader] string authorization)
        {
            var imageDataResponse = await _iotCentralApi.GetCameraLatestTelemetry(deviceId, authorization);
            var imageData = JsonConvert.DeserializeObject<dynamic>(await imageDataResponse.Content.ReadAsStringAsync());
            var results = imageData.results;

            if (results.Count == 0)
                return NotFound();

            var cameraDatas = results[0].CameraDatas;

            using var zipStream = new MemoryStream();

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);

            foreach (var cameraData in cameraDatas)
            {
                using var fileStream = new MemoryStream();
                var imagePath = cameraData.BlobUri.ToString();
                await GetImageStream(fileStream, imagePath);

                if (fileStream.Length == 0) continue;

                var zipEntry = archive.CreateEntry(GetFileName(imagePath), CompressionLevel.Fastest);

                using var zipEntryStream = zipEntry.Open();
                await fileStream.CopyToAsync(zipEntryStream);
            }

            if (zipStream.Length == 0)
                return NotFound();

            archive.Dispose(); //to finish up zip processing before sending it to client

            return File(zipStream.ToArray(), MediaTypeNames.Application.Zip, "images.zip");
        }

        private async Task GetImageStream(MemoryStream fileStream, string imagePath)
        {
            var connString = _config.GetConnectionString(Constants.BlobConnString);

            var client = new BlobServiceClient(connString);

            var container = client.GetBlobContainerClient("$web");
            var blobClient = container.GetBlobClient(imagePath);

            if (!blobClient.Exists()) return;

            await blobClient.DownloadToAsync(fileStream);
            fileStream.Position = 0;
        }

        private static string GetFileName(string imagePath)
        {
            var pathArray = imagePath.Split(@"\");
            return pathArray[2] + ".jpg";
        }

        [HttpGet("iotcentral/{deviceId}/property")]
        public async Task<ActionResult> GetIotCentralDeviceProperty(string deviceId, [FromHeader] string authorization)
        {
            var responseMessage = await _iotCentralApi.GetDeviceProperty(deviceId, authorization);

            return await HandleResponseMessage(responseMessage);
        }

        private bool ValidateDate(string dateStr)
        {
            var regex = new Regex(@"\d\d\d\d-\d\d-\d\d");

            return regex.Match(dateStr).Success;
        }


    }
}