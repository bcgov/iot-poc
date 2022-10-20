// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.Nonce
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System.Xml.Serialization;

namespace OnvifCamera.CustomUsernameToken
{
    public class Nonce
    {
        [XmlAttribute(AttributeName = "EncodingType")]
        public string EncodingType { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
