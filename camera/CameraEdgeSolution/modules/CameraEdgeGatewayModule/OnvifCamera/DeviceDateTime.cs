// Decompiled with JetBrains decompiler
// Type: OnvifCamera.DeviceDateTime
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;

namespace OnvifCamera
{
    public struct DeviceDateTime
    {
        public bool NTPEnabled { get; set; }

        public bool DHCPEnabled { get; set; }

        public IReadOnlyList<NetworkConfig> NTPSource { get; set; }

        public DateTime Time { get; set; }

        public string TimeZone { get; set; }

        public bool DaylightSavingsEnabled { get; set; }
    }
}
