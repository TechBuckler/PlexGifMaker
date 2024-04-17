using Microsoft.Extensions.Logging;
using Moq;
using PlexGifMaker.Data;
using System.Net;

namespace PlexGifMaker.Tests
{
    public class PlexServiceTests
    {
        [Fact]
        public async Task GetEpisodesAsync_ReturnsCorrectEpisodesCount()
        {
            // Arrange
            var expectedUri = new Uri($"http://test.com/library/metadata/0/allLeaves?X-Plex-Token=");

            var handlerMock = PlexServiceTestsHelpers.SetupMockHttpMessageHandler(PlexServiceTestsHelpers.Content, HttpStatusCode.OK);
            var httpClientFactoryMock = PlexServiceTestsHelpers.SetupMockHttpClientFactory(handlerMock);
            var loggerMock = new Mock<ILogger<PlexService>>();
            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);

            // Act
            var episodes = await service.GetEpisodesAsync("0", false);
            Assert.Equal(5, episodes.Count);
            PlexServiceTestsHelpers.VerifyMockHttpMessageHandler(handlerMock, expectedUri.ToString());
        }

        [Fact]
        public async Task GetLibraries()
        {
            var handlerMock = PlexServiceTestsHelpers.SetupMockHttpMessageHandler(PlexServiceTestsHelpers.Content1, HttpStatusCode.OK);
            var httpClientFactoryMock = PlexServiceTestsHelpers.SetupMockHttpClientFactory(handlerMock);
            var loggerMock = new Mock<ILogger<PlexService>>();
            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);
            
            var libraries = await service.GetLibrariesAsync();
            Assert.NotNull(libraries);
            Assert.Equal(2, libraries.Count);
        }
    }
}