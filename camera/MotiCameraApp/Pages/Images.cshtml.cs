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
using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace MotiCameraApp.Pages
{
    public class ImagesModel : PageModel
    {
        private readonly IConfiguration Configuration;

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

        public static string LastCapturedTime { get; set; } 
        public static List<string> LatestFiles { get; set; }
        public static string ImageStr { get; set; }

        public static string ImageStrHome { get; set; }
        public static string ImageStrEast { get; set; }
        public static string ImageStrWest { get; set; }
        public static string ImageStrNorth { get; set; }

        public static string ImageStrSouth { get; set; }

        //// The container we will create.
        //Container container;

        // The name of the database and container we will create
        string databaseId = "cameraDatabase";
        string containerId = "imagesContainer";

        public static List<IotHubToDeviceNotification> Images = new List<IotHubToDeviceNotification>();



        private readonly ILogger<ImagesModel> _logger;

        string blobAccountName;
        string blobContainerName;
        string blobName;
        string blobConnectionString;
        string blobAccountKey;

        public ImagesModel(IConfiguration configuration, ILogger<ImagesModel> logger)
        {
            Configuration = configuration;
            blobAccountName = Configuration["BlobAccountName"];
            blobContainerName = Configuration["BlobContainerName"];
            blobName = Configuration["BlobName"];
            blobAccountKey = Configuration["BlobAccountKey"];
            blobConnectionString = configuration["BlobConnectionString"];

            _logger = logger;
        }

        public async Task OnGet()
        { 
            await AccessBlob(blobAccountName, blobContainerName, blobName);
        }

        static async Task UploadBlob(string accountName, string containerName, string blobName, string blobContents)
        {
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        accountName,
                                                        containerName);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                                            new DefaultAzureCredential());

            try
            {
                // Create the container if it does not exist.
                await containerClient.CreateIfNotExistsAsync();

                // Upload text to a new block blob.
                byte[] byteArray = Encoding.ASCII.GetBytes(blobContents);

                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    await containerClient.UploadBlobAsync(blobName, stream);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        async Task AccessBlob(string accountName, string containerName, string blobName)
        {
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        accountName,
                                                        containerName);
  
    
            StorageSharedKeyCredential sasKey = new StorageSharedKeyCredential(accountName, blobAccountKey);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                                            sasKey);

            Dictionary<string, string> dicPresetImage = new Dictionary<string, string>();
            try
            {



                Console.WriteLine("Listing blobs...");
                int count = 0;

                List<string> fileList = new List<string>();
                // List all blobs name in the container
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.Contains(".jpg"))
                    {
                        fileList.Add(blobItem.Name);
                        if (blobItem.Name.ToLower().Contains("/home/"))
                        {
                            if (dicPresetImage.Keys.Contains("home"))
                            {
                                dicPresetImage["home"] = blobItem.Name;
                            }
                            else
                            {
                                dicPresetImage.Add("home", blobItem.Name);
                            }
                        }
                        if (blobItem.Name.ToLower().Contains("/west/"))
                        {
                            if (dicPresetImage.Keys.Contains("west"))
                            {
                                dicPresetImage["west"] = blobItem.Name;
                            }
                            else
                            {
                                dicPresetImage.Add("west", blobItem.Name);
                            }
                        }
                        if (blobItem.Name.ToLower().Contains("/north/"))
                        {
                            if (dicPresetImage.Keys.Contains("north"))
                            {
                                dicPresetImage["north"] = blobItem.Name;
                            }
                            else
                            {
                                dicPresetImage.Add("north", blobItem.Name);
                            }
                        }
                        if (blobItem.Name.ToLower().Contains("/east/"))
                        {
                            if (dicPresetImage.Keys.Contains("east"))
                            {
                                dicPresetImage["east"] = blobItem.Name;
                            }
                            else
                            {
                                dicPresetImage.Add("east", blobItem.Name);
                            }
                        }
                        if (blobItem.Name.ToLower().Contains("/south_655/"))
                        {
                            if (dicPresetImage.Keys.Contains("south"))
                            {
                                dicPresetImage["south"] = blobItem.Name;
                            }
                            else
                            {
                                dicPresetImage.Add("south", blobItem.Name);
                            }
                        }
                    }


                        

                }

                //Get latest image
                string fileName = fileList[fileList.Count - 1];
                Azure.Storage.Blobs.BlobClient blobClient = new Azure.Storage.Blobs.BlobClient(
                    connectionString: blobConnectionString,
                    blobContainerName: blobContainerName,
                    blobName: fileName
                    );
                FileName = fileName;
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobConnectionString);
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                using (MemoryStream ms = new MemoryStream())
                {
                    await cloudBlockBlob.DownloadToStreamAsync(ms);
                    string filePath = Startup.ContentRoot + "\\wwwroot\\test-1.jpg";
                    DateTime dt = System.IO.File.GetLastWriteTime(filePath);
                    LastCapturedTime = cloudBlockBlob.Properties.LastModified.Value.ToLocalTime().ToString();// dt.ToString();
                    byte[] bytes = new byte[ms.Length];
                    ms.Read(bytes, 0, (int)ms.Length);
                    var str = "";
                    str = Convert.ToBase64String(bytes);
                    Image img = Image.FromStream(ms);
                    byte[] arr;
                    using (MemoryStream ms_1 = new MemoryStream())
                    {
                        img.Save(ms_1, System.Drawing.Imaging.ImageFormat.Jpeg);
                        arr = ms.ToArray();
                    }

                    str = Convert.ToBase64String(arr);

                    ImageStr = str;

                }



                foreach (var key in dicPresetImage.Keys)
                {
                    cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(dicPresetImage[key]);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        await cloudBlockBlob.DownloadToStreamAsync(ms);
                        string filePath = Startup.ContentRoot + "\\wwwroot\\" + dicPresetImage[key].Replace("/", "\\");//test-"+ key +".jpg";
                        DateTime dt = System.IO.File.GetLastWriteTime(filePath);
                        LastCapturedTime = cloudBlockBlob.Properties.LastModified.Value.ToLocalTime().ToString();// dt.ToString();
                        byte[] bytes = new byte[ms.Length];
                        ms.Read(bytes, 0, (int)ms.Length);
                        var str = "";
                        str = Convert.ToBase64String(bytes);
                        Image img = Image.FromStream(ms);
                        byte[] arr;
                        using (MemoryStream ms_1 = new MemoryStream())
                        {
                            img.Save(ms_1, System.Drawing.Imaging.ImageFormat.Jpeg);
                            arr = ms.ToArray();
                        }

                        str = Convert.ToBase64String(arr);
                        switch (key)
                        {
                            case "home":
                                ImageStrHome = str;
                                break;

                            case "west":
                                ImageStrWest = str;
                                break;

                            case "east":
                                ImageStrEast = str;
                                break;

                            case "north":
                                ImageStrNorth = str;
                                break;

                            case "south":
                                ImageStrSouth = str;
                                break;

                            default:
                                ImageStrHome = str;
                                break;
                        }


                    }

                }




                for (int i = fileList.Count - 1; i > 0; i--)
                {
                    count++;
                    
                    
                }

                //get camera info from cosmosdb
                await GetCameraInfo();

                Console.WriteLine("done");

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task GetCameraInfo()
        {
            // Create a new instance of the Cosmos Client
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            try
            {
                Console.WriteLine("Beginning operations...\n");
           
                await GetStartedDemoAsync();

            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");

            }

        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        public async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        public async Task RetrieveDatabaseAsync()
        {

            var cosmosDatabase = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Container container = await GetOrCreateContainerAsync(cosmosDatabase, containerId);

          

            await QueryPartitionedContainerInParallelAsync(container);


        }

        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            //ADD THIS PART TO YOUR CODE
            //await this.CreateDatabaseAsync();

            await this.RetrieveDatabaseAsync();
        }

        private static async Task<Container> GetOrCreateContainerAsync(Database database, string containerId)
        {
            ContainerProperties containerProperties = new ContainerProperties(id: containerId, partitionKeyPath: "/id");

            return await database.CreateContainerIfNotExistsAsync(
                containerProperties: containerProperties,
                throughput: 400);
        }



        // <QueryPartitionedContainerInParallelAsync>
        private static async Task QueryPartitionedContainerInParallelAsync(Container container)
        {
            List<IotHubToDeviceNotification> familiesSerial = new List<IotHubToDeviceNotification>();
            string queryText = "SELECT * FROM c";

            // 0 maximum parallel tasks, effectively serial execution
            QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100 };
            options.MaxConcurrency = 0;
            using (FeedIterator<IotHubToDeviceNotification> query = container.GetItemQueryIterator<IotHubToDeviceNotification>(
                queryText,
                requestOptions: options))
            {
                if(query.HasMoreResults)
                {
                    var cameras = await query.ReadNextAsync();
                    foreach (IotHubToDeviceNotification family in cameras)
                    {
                        familiesSerial.Add(family);
                    }
                    Images = familiesSerial;
                }
            }

         }

    }



    public class IotHubToDeviceNotification
    {

        public string DeviceId { get; set; }
        public string BlobUri { get; set; }
        public string BlobName { get; set; }
        public DateTimeOffset? LastUpdatedTime { get; set; }
        public long BlobSizeInBytes { get; set; }
        public System.DateTime EnqueuedTimeUtc { get; set; }

        public CameraInfo CameraInfo;

    }

    public class CameraInfo
    {
        public bool DHCPEnabled { get; set; }
        public IReadOnlyList<string> DNSIPAddress { get; set; }
        public string Firmware { get; set; }
        public string HardwareId { get; set; }
        public string Hostname { get; set; }
        public IReadOnlyList<string> MACAddresses { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
    }
}
