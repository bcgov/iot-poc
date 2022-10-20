using Microsoft.AspNetCore.Mvc;

namespace IotApis.Controllers
{
    public class IotControllerBase : ControllerBase
    {
        protected async Task<ActionResult> HandleResponseMessage(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                var response = Ok(await responseMessage.Content.ReadAsStreamAsync());
                response.ContentTypes.Add("application/json; charset=utf-8");
                return response;
            }

            var content = await responseMessage.Content.ReadAsStringAsync();
            return StatusCode(((int)responseMessage.StatusCode), content);
        }
    }
}
