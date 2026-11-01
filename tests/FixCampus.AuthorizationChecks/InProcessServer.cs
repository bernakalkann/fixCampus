using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

// Runs the real ASP.NET Core middleware and MVC pipeline without opening a TCP socket.
// Useful in restricted environments; production continues to use Kestrel.
internal sealed class InProcessServer : IServer
{
    public IFeatureCollection Features { get; } = new FeatureCollection();
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _send;

    public InProcessServer()
    {
        var addresses = new ServerAddressesFeature();
        addresses.Addresses.Add("http://localhost");
        Features.Set<IServerAddressesFeature>(addresses);
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        _send = async (request, ct) =>
        {
            var context = new DefaultHttpContext();
            var uri = request.RequestUri!;
            context.Request.Method = request.Method.Method;
            context.Request.Scheme = uri.Scheme;
            context.Request.Host = new HostString(uri.Host, uri.Port);
            context.Request.Path = uri.AbsolutePath;
            context.Request.QueryString = new QueryString(uri.Query);
            context.Request.Protocol = "HTTP/1.1";
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            context.RequestAborted = ct;
            foreach (var header in request.Headers)
                context.Request.Headers[header.Key] = header.Value.ToArray();
            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(ct);
                context.Request.Body = new MemoryStream(bytes);
                context.Request.ContentLength = bytes.Length;
                foreach (var header in request.Content.Headers)
                    context.Request.Headers[header.Key] = header.Value.ToArray();
            }
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new BodyDetection(request.Content is not null));
            using var body = new MemoryStream();
            context.Response.Body = body;
            var appContext = application.CreateContext(context.Features);
            Exception? error = null;
            try
            {
                await application.ProcessRequestAsync(appContext);
                var response = new HttpResponseMessage((HttpStatusCode)context.Response.StatusCode)
                { Content = new ByteArrayContent(body.ToArray()), RequestMessage = request };
                foreach (var header in context.Response.Headers)
                    if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                        response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                return response;
            }
            catch (Exception ex) { error = ex; throw; }
            finally { application.DisposeContext(appContext, error); }
        };
        return Task.CompletedTask;
    }

    public HttpMessageHandler CreateHandler() => new Handler(this);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Dispose() { }

    private sealed class Handler(InProcessServer server) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => server._send!(request, ct);
    }
    private sealed class BodyDetection(bool canHaveBody) : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => canHaveBody;
    }
}
