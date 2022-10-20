using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace MotiCameraApp.Pages
{
    public class ImageApisModel : PageModel
    {

        public string FileName { get; set; }
        public string FileUrl { get; set; }

        // The Azure Cosmos DB endpoint for running this sample.
        string EndpointUri = @"https://cosmosdb-motitest.documents.azure.com:443/";
        // The primary key for the Azure Cosmos account.
        string PrimaryKey = "wKQ2Ib6d3pHnkKhmj5UfRcHteshziqktXtlZuPvI2MKDDPP50fwGebxMeiTYgk264D9yHcsUBFc5wrkLHeftFA==";


        // The Cosmos client instance
        CosmosClient cosmosClient;

        // The database we will create
        Database database;

        //// The container we will create.
        //Container container;

        // The name of the database and container we will create
        string databaseId = "cameraDatabase";
        string containerId = "imagesContainer";

        public static List<IotHubToDeviceNotification> Images = new List<IotHubToDeviceNotification>();


        private readonly ILogger<ImageApisModel> _logger;

        public ImageApisModel(ILogger<ImageApisModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            await GetImagesFromApi();


        }

        static async Task GetImagesFromApi()
        {
            
        }

        

    }



}
