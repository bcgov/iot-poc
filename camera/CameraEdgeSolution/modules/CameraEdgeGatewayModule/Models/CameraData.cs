namespace OnvifModule
{
    public class CameraData
    {
        
        public string DeviceID {get; set;}
        public string CameraID {get; set;}
        public string BlobUri {get; set;}
        public string BlobName {get; set;}
        public string LastUpdatedTime {get; set;}
        public string BlobSizeInBytes {get; set;}
        public string EnqueuedTimeUtc {get; set;}

        public string EventProcessedUtcTime {get; set;}
    }
}