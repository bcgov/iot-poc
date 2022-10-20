namespace OnvifModule
{
    using System.Collections.Generic;
    public class CameraMetadata
    {
        public string DeviceID {get; set;}
        public string MAC {get; set;}
        public string IPAddress {get; set;}
        public string Longitude {get; set;}
        public string Latitude {get; set;}
        public string Elevation {get; set;}
        public string BusinessArea {get; set;}

        public string Region {get; set;}

        public string CameraMake {get; set;}

        public List<MotiCameraInfo> Cameras {get; set;}

    }
}