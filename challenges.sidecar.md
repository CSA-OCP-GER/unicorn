# Create a deployment using Sidecar-Pattern

## Here is what you learn

- Create a deployment with an additional pod that acts as proxy for your API pod.
- Your API pod is only accessible from the proxy.

The demo the sidecar pattern an [application](https://github.com/aspnet/Proxy) is already implemented. The application consists of two components that are implemented using ASP.NET Core.
The first component SidecarProxy acts, as implied by its name, as proxy and forwards all requests to the second component DemoApi. To do all the forwarding stuff the SidecarProxy uses [Microsoft.AspNetCore.Proxy](https://github.com/aspnet/Proxy). The SidecarProxy is running on port 8080.
To have a good demo usecase the SidecarProxy injects some additional http headers when the request is forwarded to the DemoApi.
The DemoApi just echos these headers.
