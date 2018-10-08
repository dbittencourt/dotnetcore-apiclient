using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIClient
{
    public interface IClient
    {
        Task<string> RequestAsync(string address, IDictionary<string, string> optional = null);
        Task<T> RequestAsync<T>(string address, IDictionary<string, string> optional = null, bool handleType = false) 
            where T : class;
    }
}