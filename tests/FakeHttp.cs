using System.Net;
internal sealed class FakeHttp : HttpMessageHandler, IHttpClientFactory
{
    public int Requests;
    public string Body = "";
    public HttpStatusCode Status = HttpStatusCode.OK;
    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Requests);
        return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body) });
    }
}
