// Decompiled with JetBrains decompiler
// Type: OnvifCamera.DNSInfo
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System.Collections.Generic;

namespace OnvifCamera
{
    public struct DNSInfo
    {
        public bool DHCPEnabled { get; set; }

        public IReadOnlyList<string> SearchDomain { get; set; }

        public IReadOnlyList<string> DNSIPAddress { get; set; }
    }
}
