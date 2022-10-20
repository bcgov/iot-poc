using Azure.Storage.Blobs;
using IotCommon;
using Microsoft.AspNetCore.Mvc;

namespace IotApis.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImageController : IotControllerBase
    {
        private IConfiguration _config;

        public ImageController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet()]
        public async Task<IActionResult> GetImage(string imagePath)
        {
            var connString = _config.GetConnectionString(Constants.BlobConnString);

            var client = new BlobServiceClient(connString);

            var (validPath, containerName, blobName) = ParseImagePath(imagePath);
            if (!validPath) return BadRequest();

            var container = client.GetBlobContainerClient(containerName);
            var blobClient = container.GetBlobClient(blobName);

            if (!blobClient.Exists())
                return NotFound();

            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;

            return File(stream, "image/jpeg");
        }

        private (bool validPath, string containerName, string blobName) ParseImagePath(string imagePath)
        {
            Uri uri;

            try
            {
                uri = new Uri(imagePath);
            }
            catch
            {
                return (false, "", "");
            }

            return (true, uri.Segments[1], uri.LocalPath[uri.Segments[1].Length..]);
        }
    }
}
