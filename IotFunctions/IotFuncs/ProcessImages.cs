using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using IotCommon;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace IotFuncs
{
    public static class ProcessImages
    {
        [FunctionName("ResizeAndAddWatermark")]
        public static async Task Run(
            [BlobTrigger("images-src/{name}")] BlobClient srcClient,
            string name,
            ILogger log)
        {
            BlobProperties srcProps = await srcClient.GetPropertiesAsync();

            var lastModified = DateUtils.ConvertUtcToPacificTime(srcProps.LastModified.UtcDateTime).ToString("yyyy-MM-dd HH:mm");
            var font = SystemFonts.CreateFont("Arial", 10);
            var text = "Ministry of Transportation and Infrastructure";

            using var source = await srcClient.OpenReadAsync();
            using var input = Image.Load<Rgba32>(source, out IImageFormat format);

            input.Mutate(x => x.Resize(400, 0));
            input.Mutate(x => x.ApplyWaterMark(lastModified, font, Color.White, Position.Top));
            input.Mutate(x => x.ApplyWaterMark(text, font, Color.White, Position.Bottom));

            var connString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var client = new BlobServiceClient(connString);
            var container = client.GetBlobContainerClient("$web");
            var blobClient = container.GetBlobClient(name);

            var stream = new MemoryStream();
            await input.SaveAsync(stream, format);
            stream.Position = 0;

            await blobClient.UploadAsync(stream);

            log.LogInformation($"{name} has been processed.");
        }
    }   
}
