using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SidecarProxy
{
    public class BackChannelHandler : HttpClientHandler
    {
        public BackChannelHandler()
        {
            base.AllowAutoRedirect = true;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Injected-Header-One", "Value-One");
            request.Headers.Add("Injected-Header-Two", "Value-Two");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
