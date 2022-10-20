using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using RtspClientSharp;
using RtspClientSharp.RawFrames.Audio;
using RtspClientSharp.RawFrames.Video;
using RtspClientSharp.Rtsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MotiCameraApp.Pages
{
    public class VideosModel : PageModel
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }

        // The Azure Cosmos DB endpoint for running this sample.
        string EndpointUri = @"https://cosmosdb-motitest.documents.azure.com:443/";
        // The primary key for the Azure Cosmos account.
        string PrimaryKey = "wKQ2Ib6d3pHnkKhmj5UfRcHteshziqktXtlZuPvI2MKDDPP50fwGebxMeiTYgk264D9yHcsUBFc5wrkLHeftFA==";




        //// The container we will create.
        //Container container;

        // The name of the database and container we will create
        string databaseId = "cameraDatabase";
        string containerId = "imagesContainer";

        private readonly ILogger<VideosModel> _logger;

        public VideosModel(ILogger<VideosModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            string accountName = "imageribhedg6dsazu";
            string containerName = "$web";
            string blobName = "test.mp4";
            await AccessBlob(accountName, containerName, blobName);
        }

        async Task AccessBlob(string accountName, string containerName, string blobName)
        {
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        accountName,
                                                        containerName);
            string accountKey = "qf2yzOpOmzZP6sp4PP93lzF7fMJXXW63ff/+PxjuwRlDKt8Oc7sWsEdmB40X+I5N23H69krCluqGoG9n9I4m5Q==";
            StorageSharedKeyCredential sasKey = new StorageSharedKeyCredential(accountName, accountKey);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                                            sasKey);

            try
            {
                var connectionString = "<change to the real connection string>";

                Console.WriteLine("Listing blobs...");

                // List all blobs in the container
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    string fileName = blobItem.Name;
                    Azure.Storage.Blobs.BlobClient blobClient = new Azure.Storage.Blobs.BlobClient(
                        connectionString: connectionString,
                        blobContainerName: "$web",
                        blobName: fileName
                        );
                    if (fileName.Contains(".mp4"))
                    {
                        FileName = fileName;
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                        CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                        CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                        CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            await cloudBlockBlob.DownloadToStreamAsync(ms);
                            string filePath = Startup.ContentRoot + "\\wwwroot\\test-1.mp4";
                            byte[] bytes = new byte[ms.Length];
                            ms.Read(bytes, 0, (int)ms.Length);
                            using (var fs = new FileStream(filePath, FileMode.Create))
                            {
                                ms.WriteTo(fs);
                            }
                        }
                    }
                }



                Console.WriteLine("done");

            }
            catch (Exception e)
            {
                throw e;
            }
        }


    }
}
