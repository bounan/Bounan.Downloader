using System.Net;
using System.Text;
using Bounan.Downloader.Worker.Clients;
using NUnit.Framework;

namespace Bounan.Downloader.Worker.Tests.Clients;

[TestFixture]
public class ShikimoriClientTests
{
    [Test]
    public async Task GetTitleAsync_ReturnsRussian_WhenRussianPresent()
    {
        const string json = """
                            {
                              "data": {
                                "animes": [
                                  {
                                    "id": "1",
                                    "russian": "Ковбой Бибоп",
                                    "name": "Cowboy Bebop"
                                  }
                                ]
                              }
                            }
                            """;

        using var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }));

        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var factory = new FakeHttpClientFactory(httpClient);

        var sut = new ShikimoriClient(factory);
        var title = await sut.GetTitleAsync(1, CancellationToken.None);

        Assert.That(title, Is.EqualTo("Ковбой Бибоп"));
    }

    [Test]
    public async Task GetTitleAsync_ReturnsName_WhenRussianMissingOrEmpty()
    {
        const string json = """
                            {
                              "data": {
                                "animes": [
                                  {
                                    "id": "1",
                                    "russian": "",
                                    "name": "Cowboy Bebop"
                                  }
                                ]
                              }
                            }
                            """;

        using var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }));

        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var factory = new FakeHttpClientFactory(httpClient);

        var sut = new ShikimoriClient(factory);
        var title = await sut.GetTitleAsync(1, CancellationToken.None);

        Assert.That(title, Is.EqualTo("Cowboy Bebop"));
    }

    [Test]
    public void GetTitleAsync_ThrowsInvalidOperation_WhenAnimeNotFound()
    {
        const string json = "{\"data\":{\"animes\":[]}}";

        using var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }));

        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var factory = new FakeHttpClientFactory(httpClient);

        var sut = new ShikimoriClient(factory);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.GetTitleAsync(21, CancellationToken.None));
    }

    [Test]
    public void GetTitleAsync_ThrowsInvalidOperation_WhenWrongId()
    {
        const string json = """
                            {
                              "data": {
                                "animes": [
                                  {
                                    "id": "2",
                                    "russian": "Ковбой Бибоп",
                                    "name": "Cowboy Bebop"
                                  }
                                ]
                              }
                            }
                            """;

        using var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }));

        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var factory = new FakeHttpClientFactory(httpClient);

        var sut = new ShikimoriClient(factory);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.GetTitleAsync(21, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("Received wrong anime"));
    }

    [Test]
    public void GetTitleAsync_ThrowsHttpRequestException_OnNonSuccessStatus()
    {
        using var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var factory = new FakeHttpClientFactory(httpClient);

        var sut = new ShikimoriClient(factory);

        Assert.ThrowsAsync<HttpRequestException>(async () => await sut.GetTitleAsync(21, CancellationToken.None));
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class TestHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responder(request, cancellationToken);
        }
    }
}
