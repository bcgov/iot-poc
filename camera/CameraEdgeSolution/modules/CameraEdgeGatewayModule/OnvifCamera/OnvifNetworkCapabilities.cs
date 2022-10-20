// Decompiled with JetBrains decompiler
// Type: OnvifCamera.OnvifNetworkCapabilities
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

namespace OnvifCamera
{
    public struct OnvifNetworkCapabilities
    {
        public bool DHCPv6 { get; set; }

        public bool DHCPv6Enabled { get; set; }

        public bool HostnameFromDHCP { get; set; }

        public int NTPCount { get; set; }

        public bool NTPEnabled { get; set; }

        public bool IPVersion6 { get; set; }

        public bool IPVersion6Enabled { get; set; }

        public bool DynamicDNS { get; set; }

        public bool DynamicDNSEnabled { get; set; }

        public bool WSDDiscoveryEnabled { get; set; }

        public bool ZeroConfiguration { get; set; }

        public bool ZeroConfigurationEnabled { get; set; }
    }
}
