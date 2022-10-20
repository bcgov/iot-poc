// Decompiled with JetBrains decompiler
// Type: OnvifCamera.SourceConfig
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

namespace OnvifCamera
{
    public struct SourceConfig
    {
        public string Name { set; get; }

        public string SourceToken { get; set; }

        public string SourceConfigToken { get; set; }

        public int UseCount { get; set; }

        public string ViewMode { set; get; }

        public System.Drawing.Rectangle Bounds { set; get; }
    }
}
