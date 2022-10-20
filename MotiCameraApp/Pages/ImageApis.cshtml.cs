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
using System.Net.Http;
using Newtonsoft.Json;
using IoTCentral;
using System.IO.Compression;
using System.Drawing;

namespace MotiCameraApp.Pages
{

    public class ImageHomeInfo : ImageInfo
    {
        public string ImageStrHome;
    }
    public class ImageWestInfo : ImageInfo
    {
        public string ImageStrWest;
    }
    public class ImageEastInfo : ImageInfo
    {
        public string ImageStrEast;
    }
    public class ImageNorthInfo : ImageInfo
    {
        public string ImageStrNorth;
    }

    public class ImageSouthInfo : ImageInfo
    {
        
        public string ImageStrSouth;
    }


    public class ImageInfo
    {
        public string CameraId;
        public string PreSet;
        public string ImageStr;

    }

    public class CameraImage
    {
        public string CameraId;
        public string ImageStrHome;
        public string ImageStrWest;
        public string ImageStrEast;
        public string ImageStrNorth;
        public string ImageStrSouth;

        public string ImageStr;
        public string PreSet;

    }

    public class ImageApisModel : PageModel
    {
        public static List<string> LatestFiles { get; set; }
        public static string ImageStr { get; set; }
        public static string ImageStrHome { get; set; }
        public static string ImageStrEast { get; set; }
        public static string ImageStrWest { get; set; }
        public static string ImageStrNorth { get; set; }
        public static string ImageStrSouth { get; set; }
        public static ImageHomeInfo ImageHome { get; set; }
        public static ImageWestInfo ImageWest { get; set; }
        public static ImageEastInfo ImageEast { get; set; }
        public static ImageNorthInfo ImageNorth { get; set; }
        public static ImageSouthInfo ImageSouth { get; set; }
        public static List<ImageInfo> AllImages { get; set; }
        public static List<CameraImage> AllCameraImages { get; set; }
        public static List<CameraPresetImages> ImagesPerCamera { get; set; }
        public static string ErrotStr { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public static List<IotHubToDeviceNotification> Images = new List<IotHubToDeviceNotification>();
        private readonly ILogger<ImageApisModel> _logger;

        public ImageApisModel(ILogger<ImageApisModel> logger)
        {
            _logger = logger;
            ImagesPerCamera = new List<CameraPresetImages>();
        }

        public async Task OnGet()
        {
            LatestFiles = new List<string>();
            AllCameraImages = new List<CameraImage>();
            AllImages = new List<ImageInfo>();
            List<string> cameraIds = await GetDevicesFromApi();
            foreach (string id in cameraIds)
            {
                await GetImagesFromApi(id);
            }
            
        }


        static async Task<List<string>> GetDevicesFromApi()
        {
            string apiUrl = @"https://iot-central-for-cameras-test.azureiotcentral.com/api/devices?api-version=1.2-preview";
            var httpClient = new HttpClient();
            string accessToken = @"SharedAccessSignature sr=e61f53f1-117d-4ad9-8cfb-b691385d8751&sig=mO%2BwGTx%2FwOEH2dYUQyFj4g3jnegsIyNgAuKbRyOfi2U%3D&skn=IOTC-Wrapper&se=1688139787465";
            httpClient.DefaultRequestHeaders.Add("Authorization", accessToken);
            var streamTask = httpClient.GetStringAsync(apiUrl).Result;
            var devices = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceListTest>(streamTask);
            var cameras = devices.value.Where(x => x.template == "dtmi:modelDefinition:jipkbe6hr:g8voncqowt").Select(x => x.id).ToList();
            return cameras;
        }


        static async Task GetImagesFromApi(string id)
        {
            ErrotStr = "";
            string apiUrl = @"https://moti-iot-test.azurewebsites.net/api/camera/devices/iotcentral/" + id + @"/images/latest";
            var httpClient = new HttpClient();
            string accessToken = @"SharedAccessSignature sr=e61f53f1-117d-4ad9-8cfb-b691385d8751&sig=mO%2BwGTx%2FwOEH2dYUQyFj4g3jnegsIyNgAuKbRyOfi2U%3D&skn=IOTC-Wrapper&se=1688139787465";
            httpClient.DefaultRequestHeaders.Add("Authorization", accessToken);

            using (HttpResponseMessage response = await httpClient.GetAsync(apiUrl))
            {
                string zipFileName = "test-" + id + ".zip";
                System.IO.FileStream fs = new FileStream(Startup.ContentRoot + "/" + zipFileName, FileMode.Create);

                var zipFile = response.Content.CopyToAsync(fs);
                System.Threading.Thread.Sleep(3000);
                fs.Close();

                try
                {
                    ErrotStr += fs.Name;
                    using (ZipArchive zip = ZipFile.OpenRead(fs.Name))
                    {
                        var images = zip.Entries.Where(x => !x.Name.Contains("mp4")).ToList();
                        foreach (ZipArchiveEntry image in images)
                        {
                            try
                            {
                                image.ExtractToFile(image.Name, true);
                            }
                            catch (Exception exc)
                            {
                            }

                            using (FileStream imageFile = new FileStream(image.Name, FileMode.Open))
                            {
                                byte[] bytes = new byte[image.Length];
                                imageFile.Read(bytes, 0, (int)imageFile.Length);
                                var str = "";
                                str = Convert.ToBase64String(bytes);
                                ImageStr = str;
                                var preSet = image.Name.Substring(0, image.Name.IndexOf("."));
                                CameraImage ci = new CameraImage { CameraId = id, ImageStr = str, PreSet = preSet };
                                AllCameraImages.Add(ci);

                            }
                        }

                        var cameraIds = AllCameraImages.Select(x => x.CameraId).Distinct().Where(x => x == id).ToList();
                        foreach (var cameraId in cameraIds)
                        {
                            var camera = new CameraPresetImages { CameraId = cameraId, PresetImageDic = new Dictionary<string, string>() };
                            var presetImages = AllCameraImages.Where(x => x.CameraId == cameraId).ToList();
                            foreach (var i in presetImages)
                            {
                                camera.PresetImageDic.Add(i.PreSet, i.ImageStr);
                            }
                            ImagesPerCamera.Add(camera);
                        }

                    }
                }
                catch (System.IO.IOException ioexc)
                {
                    ErrotStr += ioexc.Message;
                    ErrotStr += " ioexc";

                }
                catch (System.IO.InvalidDataException idataexc)
                {
                    ErrotStr += "\n";
                    ErrotStr += idataexc.Message;
                    ErrotStr += " idataexc";
                    ErrotStr += "\n";
                    ErrotStr += idataexc.StackTrace;

                }
                catch (Exception exc)
                {
                    ErrotStr += exc.Message;


                }

            }

        }


        public class DeviceListTest
        {
            public List<DeviceTest> value;

        };
        public class DeviceTest
        {
            public string id;
            public string displayName;
            public bool simulated;
            public bool provisioned;
            public string etag;
            public string template;
            public bool enabled;
        }


        public class CameraPresetImages
        {
            public string CameraId;
            public Dictionary<string, string> PresetImageDic;
        }
    }



}
