// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.InterceptHttpMessageHandler
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OnvifCamera.CustomUsernameToken
{
    public class InterceptHttpMessageHandler : DelegatingHandler
    {
        public InterceptHttpMessageHandler(HttpMessageHandler innerHandler) => this.InnerHandler = innerHandler;

        protected override async Task<HttpResponseMessage> SendAsync(
          HttpRequestMessage request,
          CancellationToken cancellationToken)
        {
            request.Headers.ExpectContinue = new bool?(false);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
