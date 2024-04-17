using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PlexGifMaker.Data;
using System.Net;
using System.Net.Http;

namespace PlexGifMaker.Tests
{
    public class PlexServiceTests
    {
        [Fact]
        public async Task GetEpisodesAsync_ReturnsCorrectEpisodesCount()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("...XML content..."),
               })
               .Verifiable();

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/"),
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var loggerMock = new Mock<ILogger<PlexService>>();

            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);

            // Act
            var episodes = await service.GetEpisodesAsync("libraryKey", true);

            // Assert
            // ... your assertions here ...

            // also check the http call was like we expected it
            var expectedUri = new Uri("http://test.com/library/metadata/libraryKey/allLeaves?X-Plex-Token=your_token");

            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1), // we expected a single external request
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get  // we expected a GET request
                  && req.RequestUri == expectedUri // to this uri
               ),
               ItExpr.IsAny<CancellationToken>()
            );
        }
        [Fact]
        public async Task GetLibraries()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                   "<MediaContainer size=\"2\" allowSync=\"0\" title1=\"Plex Library\">\r\n" +
                   "<Directory allowSync=\"1\" art=\"/:/resources/movie-fanart.jpg\" composite=\"/library/sections/1/composite/1713383413\" filters=\"1\" " +
                   "refreshing=\"0\" thumb=\"/:/resources/movie.png\" key=\"1\" type=\"movie\" title=\"Movies\" agent=\"com.plexapp.agents.imdb\" " +
                   "scanner=\"Plex Movie Scanner\" language=\"en\" uuid=\"32670b35-b573-4a4c-96e2-86f973eb7cb7\" updatedAt=\"1711464805\" " +
                   "createdAt=\"1669930186\" scannedAt=\"1713383413\" content=\"1\" directory=\"1\" contentChangedAt=\"1768504\" hidden=\"0\">\r\n<Location id=\"11\" " +
                   "path=\"/home/swuser/floatingfish/NewMovies\" />\r\n<Location id=\"12\" path=\"/home/swuser/floatingcloud/Movies\" />\r\n<Location id=\"13\" " +
                   "path=\"/home/swuser/Videos\" />\r\n<Location id=\"14\" path=\"/home/swuser/Downloads/radarr\" />\r\n<Location id=\"15\" path=\"/mnt/storage/movies\" />" +
                   "\r\n<Location id=\"16\" path=\"/mnt/floatinghulk/NewMovies\" />\r\n</Directory>\r\n<Directory allowSync=\"1\" art=\"/:/resources/show-fanart.jpg\" " +
                   "composite=\"/library/sections/2/composite/1713385522\" filters=\"1\" refreshing=\"0\" thumb=\"/:/resources/show.png\" key=\"2\" type=\"show\" " +
                   "title=\"TV Shows\" agent=\"com.plexapp.agents.thetvdb\" scanner=\"Plex Series Scanner\" language=\"en\" uuid=\"ca22bbb6-386c-45e1-80bf-0c7ceb723f32\" " +
                   "updatedAt=\"1711464870\" createdAt=\"1669930350\" scannedAt=\"1713385522\" content=\"1\" directory=\"1\" contentChangedAt=\"1768492\" hidden=\"0\">\r\n" +
                   "<Location id=\"17\" path=\"/home/swuser/floatingfish/NewShows\" />\r\n<Location id=\"18\" path=\"/home/swuser/TV\" />\r\n<Location id=\"19\" " +
                   "path=\"/home/swuser/floatingcloud/TV Shows\" />\r\n<Location id=\"20\" path=\"/mnt/floatinghulk/NewShows\" />\r\n<Location id=\"21\" " +
                   "path=\"/mnt/storage/tv\" />\r\n</Directory>\r\n</MediaContainer>\r\n"),
               })
               .Verifiable();

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/"),
            };
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var loggerMock = new Mock<ILogger<PlexService>>();

            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);
            var libraries = await service.GetLibrariesAsync();
            Assert.NotNull(libraries);
            Assert.Equal(2, libraries.Count);
        }
    }

}