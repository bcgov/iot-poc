namespace IotApis.Model
{
    public class DeviceImage
    {
        public string deviceId { get; set; }
        public string id { get; set; }
        public long TimeStamp { get; set; }
        public string CameraName { get; set; }
        public string CameraAddress { get; set; }
        public string SnapshotDate { get; set; }
        public string PartitionKey { get; set; }
        public string ImagePath { get; set; }
        public string CameraModel { get; set; }
        public string CameraSerialNumber { get; set; }
        public string CameraManufacture { get; set; }
        public string PreSet { get; set; }
    }
}
