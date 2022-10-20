// Decompiled with JetBrains decompiler
// Type: OnvifCamera.UnknownVideoTypeStateException
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public class UnknownVideoTypeStateException : Exception
    {
        public UnknownVideoTypeStateException(string message)
          : base(message)
        {
        }

        public UnknownVideoTypeStateException(string message, Exception innerException)
          : base(message, innerException)
        {
        }

        public UnknownVideoTypeStateException()
        {
        }
    }
}
