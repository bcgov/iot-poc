// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.TimeOnDevice
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera.CustomUsernameToken
{
    public class TimeOnDevice
    {
        private DateTime _timeOnDvc;
        private DateTime _timeSnap;

        public TimeOnDevice(DateTime utcTimeonDev)
        {
            this._timeOnDvc = utcTimeonDev;
            this._timeSnap = DateTime.Now;
        }

        public DateTime DateTime => this._timeOnDvc + (DateTime.Now - this._timeSnap);
    }
}
