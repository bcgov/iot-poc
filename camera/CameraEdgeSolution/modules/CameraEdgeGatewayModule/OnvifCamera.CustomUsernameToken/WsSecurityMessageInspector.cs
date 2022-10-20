// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.WsSecurityMessageInspector
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace OnvifCamera.CustomUsernameToken
{
    public sealed class WsSecurityMessageInspector : IClientMessageInspector
    {
        private readonly string _username;
        private readonly string _password;
        private readonly TimeOnDevice _time;

        public WsSecurityMessageInspector(string username, string password, TimeOnDevice time)
        {
            this._username = username;
            this._password = password;
            this._time = time;
        }

        object IClientMessageInspector.BeforeSendRequest(
          ref Message request,
          IClientChannel channel)
        {
            SecurityMessageHeader header = new SecurityMessageHeader(new UsernameTokenInfo(new UserInfo(this._username, this._password, this._time.DateTime)));
            request.Headers.Add((MessageHeader)header);
            return (object)null;
        }

        void IClientMessageInspector.AfterReceiveReply(
          ref Message reply,
          object correlationState)
        {
        }
    }
}
