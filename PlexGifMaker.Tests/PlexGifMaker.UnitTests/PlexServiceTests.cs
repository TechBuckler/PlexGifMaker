using Microsoft.Extensions.Logging;
using Moq;
using PlexGifMaker.Data;
using System.Net;

namespace PlexGifMaker.Tests.PlexGifMaker.UnitTests
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
        public async Task GetLibraries_ReturnsCorrectLibraries()
        {
            // Arrange
            var baseUri = "http://test.com";
            var token = "3TcQZEzVWANSs1gs_sXs";
            var expectedUri = $"{baseUri}/library/sections?X-Plex-Token={token}";

            var handlerMock = PlexServiceTestsHelpers.SetupMockHttpMessageHandler(PlexServiceTestsHelpers.Content1, HttpStatusCode.OK);
            var httpClientFactoryMock = PlexServiceTestsHelpers.SetupMockHttpClientFactory(handlerMock);
            var loggerMock = new Mock<ILogger<PlexService>>();
            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);

            // Set the configuration
            service.SetConfiguration(baseUri, token);

            // Act
            var libraries = await service.GetLibrariesAsync();

            // Assert
            Assert.NotNull(libraries);
            Assert.Equal(2, libraries.Count);
            Assert.Equal("Movies", libraries[0].Title);
            Assert.Equal("TV Shows", libraries[1].Title);

            // Verify the correct URL was called
            PlexServiceTestsHelpers.VerifyMockHttpMessageHandler(handlerMock, expectedUri);
        }
        [Fact]
        public async Task GetShowsAsync_ReturnsCorrectShowsCount()
        {
            // Arrange
            var expectedUri = new Uri($"http://test.com/library/sections/1/all?X-Plex-Token=");
            var handlerMock = PlexServiceTestsHelpers.SetupMockHttpMessageHandler(PlexServiceTestsHelpers.Content, HttpStatusCode.OK);
            var httpClientFactoryMock = PlexServiceTestsHelpers.SetupMockHttpClientFactory(handlerMock);
            var loggerMock = new Mock<ILogger<PlexService>>();
            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);

            // Act
            var shows = await service.GetShowsAsync("1");

            // Assert
            Assert.Equal(5, shows.Count);
            PlexServiceTestsHelpers.VerifyMockHttpMessageHandler(handlerMock, expectedUri.ToString());
        }

        [Fact]
        public async Task GetSubtitleOptionsAsync_ReturnsCorrectSubtitles()
        {
            // Arrange
            var plexToken = "3TcQZEzVWANSs1gs_sXs";
            var expectedUri = new Uri($"http://test.com/library/metadata/8405?X-Plex-Token={plexToken}");
            var handlerMock = PlexServiceTestsHelpers.SetupMockHttpMessageHandler(PlexServiceTestsHelpers.Content2, HttpStatusCode.OK);
            var httpClientFactoryMock = PlexServiceTestsHelpers.SetupMockHttpClientFactory(handlerMock);
            var loggerMock = new Mock<ILogger<PlexService>>();
            var service = new PlexService(httpClientFactoryMock.Object, loggerMock.Object);

            // Set the configuration with the test token
            service.SetConfiguration("http://test.com", plexToken);

            // Act
            var subtitles = await service.GetSubtitleOptionsAsync("8405");

            // Assert
            Assert.NotNull(subtitles);
            Assert.True(subtitles.Count > 0);
            PlexServiceTestsHelpers.VerifyMockHttpMessageHandler(handlerMock, expectedUri.ToString());
        }
    }
}