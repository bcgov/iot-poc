// Decompiled with JetBrains decompiler
// Type: OnvifCamera.DeviceInformation
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System.Collections.Generic;
    using Newtonsoft.Json;

namespace OnvifCamera
{
    public struct DeviceInformation
    {
        [JsonProperty(PropertyName = "deviceId")]
        public string devideId { get; set; }

        public string PartitionKey => this.devideId;

        public string Manufacturer { get; set; }

        public string Model { get; set; }

        public string Firmware { get; set; }

        public string HardwareId { get; set; }

        public IReadOnlyList<string> MACAddresses { get; set; }

        public string SerialNumber { get; set; }

        public string Hostname { get; set; }

        public bool DHCPEnabled { get; set; }

        public IReadOnlyList<string> DNSIPAddress { get; set; }

        public bool IsCameraOn {get; set;}

        public string StreamUri {get; set;}

        public List<string> PreSets {get; set;}

        public List<string> ImageUrls {get; set;}
    }
}
