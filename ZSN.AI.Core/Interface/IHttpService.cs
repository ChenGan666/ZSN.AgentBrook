using RestSharp;

namespace ZSN.AI.Core.Interface
{
    public interface IHttpService
    {
        Task<RestResponse> PostAsync(string url, Object jsonBody);
    }
}
