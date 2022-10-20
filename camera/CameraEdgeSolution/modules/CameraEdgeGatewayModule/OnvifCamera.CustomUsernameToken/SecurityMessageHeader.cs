// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.SecurityMessageHeader
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Serialization;

namespace OnvifCamera.CustomUsernameToken
{
    public class SecurityMessageHeader : MessageHeader
    {
        public SecurityMessageHeader(UsernameTokenInfo usernameTokenInfo) => this.UsernameToken = new UsernameToken(usernameTokenInfo);

        public UsernameToken UsernameToken { get; set; }

        public override string Name => "Security";

        public override string Namespace => "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        public override bool MustUnderstand => true;

        protected override void OnWriteHeaderContents(
          XmlDictionaryWriter writer,
          MessageVersion messageVersion)
        {
            new XmlSerializer(typeof(UsernameToken)).Serialize((XmlWriter)writer, (object)this.UsernameToken);
        }
    }
}
