using Moq;
using Moq.Protected;
using System.Net;

internal static class PlexServiceTestsHelpers
{
        public const string Content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<MediaContainer size=""5"" allowSync=""1"" art=""/library/metadata/8405/art/1711547700"" identifier=""com.plexapp.plugins.library"" key=""8405"" librarySectionID=""2"" librarySectionTitle=""TV Shows"" librarySectionUUID=""ca22bbb6-386c-45e1-80bf-0c7ceb723f32"" mediaTagPrefix=""/system/bundle/media/flags/"" mediaTagVersion=""1698860922"" mixedParents=""1"" nocache=""1"" parentIndex=""1"" parentTitle=""The 10th Kingdom"" parentYear=""2000"" theme=""/library/metadata/8405/theme/1711547700"" title1=""TV Shows"" title2=""The 10th Kingdom"" viewGroup=""episode"" viewMode=""65592"">
<Video ratingKey=""8407"" key=""/library/metadata/8407"" parentRatingKey=""8406"" grandparentRatingKey=""8405"" guid=""com.plexapp.agents.thetvdb://78886/1/1?lang=en"" studio=""NBC"" type=""episode"" title=""Part 1"" grandparentKey=""/library/metadata/8405"" parentKey=""/library/metadata/8406"" grandparentTitle=""The 10th Kingdom"" parentTitle=""Season 1"" contentRating=""TV-PG"" summary=""Thanks to Relish the Troll King, the Evil Queen is once again free, and she has a trap ready for Prince Wendell. Her dog transforms into Prince Wendell and Prince Wendell becomes a dog. While trying to escape, the real Prince Wendell accidentally turns on a magic mirror, and runs away into 10th kingdom. Virginia hits him with her bicycle, feeling bad about hitting a dog; she takes him to work with her. Wolf and Relish the Troll Kings kids are after the real Prince, and they are looking all over New York for him."" index=""1"" parentIndex=""1"" viewCount=""1"" skipCount=""1"" lastViewedAt=""1670296206"" year=""2000"" thumb=""/library/metadata/8407/thumb/1711464915"" art=""/library/metadata/8405/art/1669931876"" parentThumb=""/library/metadata/8406/thumb/1711464916"" grandparentThumb=""/library/metadata/8405/thumb/1669931876"" grandparentArt=""/library/metadata/8405/art/1669931876"" grandparentTheme=""/library/metadata/8405/theme/1669931876"" duration=""5368532"" originallyAvailableAt=""2000-02-27"" addedAt=""1669931876"" updatedAt=""1711464915"" audienceRatingImage=""themoviedb://image.rating"" chapterSource=""media"">
<Media id=""14332"" duration=""5368532"" bitrate=""4161"" width=""1920"" height=""1080"" aspectRatio=""1.78"" audioChannels=""2"" audioCodec=""aac"" videoCodec=""hevc"" videoResolution=""1080"" container=""mkv"" videoFrameRate=""24p"" audioProfile=""lc"" videoProfile=""main 10"">
<Part id=""14333"" key=""/library/parts/14333/1548421685/file.mkv"" duration=""5368532"" file=""/home/swuser/floatingcloud/TV Shows/The 10th Kingdom/Season 1/The 10th Kingdom - S01E01-02 - Part 1 + Part 2.mkv"" size=""2792448569"" audioProfile=""lc"" container=""mkv"" videoProfile=""main 10"" />
</Media>
<Director tag=""David Carson"" />
<Director tag=""Herbert Wise"" />
<Writer tag=""Simon Moore"" />
<Role tag=""Kimberly Williams-Paisley"" />
<Role tag=""John Larroquette"" />
<Role tag=""Scott Cohen"" />
</Video>
<Video ratingKey=""8408"" key=""/library/metadata/8408"" parentRatingKey=""8406"" grandparentRatingKey=""8405"" guid=""com.plexapp.agents.thetvdb://78886/1/3?lang=en"" studio=""NBC"" type=""episode"" title=""Part 3"" grandparentKey=""/library/metadata/8405"" parentKey=""/library/metadata/8406"" grandparentTitle=""The 10th Kingdom"" parentTitle=""Season 1"" contentRating=""TV-PG"" summary=""While the Troll kids are taking an unconscious Virginia to their kingdom, Tony must find a way to escape the Snow White Memorial prison. News about the Evil Queen&#39;s prison break is all around the nine kingdoms. The Evil Queen rides undetected in Prince Wendell&#8217;s coach with the false Prince Wendell. When Wolf realizes he loves Virginia, he becomes determined to help her escape the Trolls and reunite her with her father."" index=""3"" parentIndex=""1"" viewOffset=""4535910"" lastViewedAt=""1679280209"" year=""2000"" thumb=""/library/metadata/8408/thumb/1711464915"" art=""/library/metadata/8405/art/1669931876"" parentThumb=""/library/metadata/8406/thumb/1711464916"" grandparentThumb=""/library/metadata/8405/thumb/1669931876"" grandparentArt=""/library/metadata/8405/art/1669931876"" grandparentTheme=""/library/metadata/8405/theme/1669931876"" duration=""5299924"" originallyAvailableAt=""2000-03-01"" addedAt=""1669931876"" updatedAt=""1711464915"" audienceRatingImage=""themoviedb://image.rating"" chapterSource=""media"">
<Media id=""14333"" duration=""5299924"" bitrate=""4164"" width=""1920"" height=""1080"" aspectRatio=""1.78"" audioChannels=""2"" audioCodec=""aac"" videoCodec=""hevc"" videoResolution=""1080"" container=""mkv"" videoFrameRate=""24p"" audioProfile=""lc"" videoProfile=""main 10"">
<Part id=""14334"" key=""/library/parts/14334/1548421685/file.mkv"" duration=""5299924"" file=""/home/swuser/floatingcloud/TV Shows/The 10th Kingdom/Season 1/The 10th Kingdom - S01E03-04 - Part 3 + Part 4.mkv"" size=""2758927228"" audioProfile=""lc"" container=""mkv"" videoProfile=""main 10"" />
</Media>
<Director tag=""Herbert Wise"" />
<Director tag=""David Carson"" />
<Writer tag=""Simon Moore"" />
<Role tag=""Kimberly Williams-Paisley"" />
<Role tag=""John Larroquette"" />
<Role tag=""Scott Cohen"" />
</Video>
<Video ratingKey=""8409"" key=""/library/metadata/8409"" parentRatingKey=""8406"" grandparentRatingKey=""8405"" guid=""com.plexapp.agents.thetvdb://78886/1/5?lang=en"" studio=""NBC"" type=""episode"" title=""Part 5"" grandparentKey=""/library/metadata/8405"" parentKey=""/library/metadata/8406"" grandparentTitle=""The 10th Kingdom"" parentTitle=""Season 1"" contentRating=""TV-PG"" summary=""The Evil Queens Huntsman has captured Virginia. She then sends a letter to Prince Wendell&#8217;s council explaining that The Prince had an injury and he is staying in his Hunting lodge to heal. In the meantime, Tony and Wolf are looking for a Magic Axe that will break a curse cast on Virginia&#8217;s hair. With the help of the Magic Birds, Virginia is free, and they finally find Acorn the Dwarf. But the mirror was sold, and Virginia, Tony and Wolf must go to the Little Lamb Village to find it."" index=""5"" parentIndex=""1"" viewCount=""1"" lastViewedAt=""1687398249"" year=""2000"" thumb=""/library/metadata/8409/thumb/1711464915"" art=""/library/metadata/8405/art/1669931876"" parentThumb=""/library/metadata/8406/thumb/1711464916"" grandparentThumb=""/library/metadata/8405/thumb/1669931876"" grandparentArt=""/library/metadata/8405/art/1669931876"" grandparentTheme=""/library/metadata/8405/theme/1669931876"" duration=""5342804"" originallyAvailableAt=""2000-03-06"" addedAt=""1669931876"" updatedAt=""1711464915"" audienceRatingImage=""themoviedb://image.rating"" chapterSource=""media"">
<Media id=""14334"" duration=""5342804"" bitrate=""4158"" width=""1920"" height=""1080"" aspectRatio=""1.78"" audioChannels=""2"" audioCodec=""aac"" videoCodec=""hevc"" videoResolution=""1080"" container=""mkv"" videoFrameRate=""24p"" audioProfile=""lc"" videoProfile=""main 10"">
<Part id=""14335"" key=""/library/parts/14335/1548421685/file.mkv"" duration=""5342804"" file=""/home/swuser/floatingcloud/TV Shows/The 10th Kingdom/Season 1/The 10th Kingdom - S01E05-06 - Part 5 + Part 6.mkv"" size=""2776847120"" audioProfile=""lc"" container=""mkv"" videoProfile=""main 10"" />
</Media>
<Director tag=""Herbert Wise"" />
<Director tag=""David Carson"" />
<Writer tag=""Simon Moore"" />
<Role tag=""Kimberly Williams-Paisley"" />
<Role tag=""John Larroquette"" />
<Role tag=""Scott Cohen"" />
</Video>
<Video ratingKey=""8410"" key=""/library/metadata/8410"" parentRatingKey=""8406"" grandparentRatingKey=""8405"" guid=""com.plexapp.agents.thetvdb://78886/1/7?lang=en"" studio=""NBC"" type=""episode"" title=""Part 7"" grandparentKey=""/library/metadata/8405"" parentKey=""/library/metadata/8406"" grandparentTitle=""The 10th Kingdom"" parentTitle=""Season 1"" contentRating=""TV-PG"" summary=""In search of The Magic Traveling Mirror, Virginia, Tony, Wolf and Prince find themselves in a strange town. Kissing town, the place where Snow White was saved from her deadly sleep by a prince&#8217;s kiss. They all begin gambling to get enough money to buy The Mirror from an auction house. Wolf hits a jackpot but doesn&#8217;t tell anyone. Wolf, determined to win Virginias love, then spends all of his money on a Singing Engagement Ring and other things for Virginia. The Huntsman outbids them at the auction and wins The Mirror. He then blackmails Tony. If Tony doesn&#39;t give him the Prince he will smash the mirror. Virginia is mad at Wolf for lying so she turns his proposal down. Tony comes out with a plan to get the mirror and save the Prince, but it fails and mirror is broken. The Evil Queen kills Relish the Troll king with a poison apple."" index=""7"" parentIndex=""1"" viewCount=""1"" lastViewedAt=""1689734667"" year=""2000"" thumb=""/library/metadata/8410/thumb/1711464915"" art=""/library/metadata/8405/art/1669931876"" parentThumb=""/library/metadata/8406/thumb/1711464916"" grandparentThumb=""/library/metadata/8405/thumb/1669931876"" grandparentArt=""/library/metadata/8405/art/1669931876"" grandparentTheme=""/library/metadata/8405/theme/1669931876"" duration=""5297747"" originallyAvailableAt=""2000-03-19"" addedAt=""1669931876"" updatedAt=""1711464915"" chapterSource=""media"">
<Media id=""14335"" duration=""5297747"" bitrate=""4193"" width=""1920"" height=""1080"" aspectRatio=""1.78"" audioChannels=""2"" audioCodec=""aac"" videoCodec=""hevc"" videoResolution=""1080"" container=""mkv"" videoFrameRate=""24p"" audioProfile=""lc"" videoProfile=""main 10"">
<Part id=""14336"" key=""/library/parts/14336/1548421685/file.mkv"" duration=""5297747"" file=""/home/swuser/floatingcloud/TV Shows/The 10th Kingdom/Season 1/The 10th Kingdom - S01E07-08 - Part 7 + Part 8.mkv"" size=""2756432042"" audioProfile=""lc"" container=""mkv"" videoProfile=""main 10"" />
</Media>
</Video>
<Video ratingKey=""8411"" key=""/library/metadata/8411"" parentRatingKey=""8406"" grandparentRatingKey=""8405"" guid=""com.plexapp.agents.thetvdb://78886/1/9?lang=en"" studio=""NBC"" type=""episode"" title=""Part 9"" grandparentKey=""/library/metadata/8405"" parentKey=""/library/metadata/8406"" grandparentTitle=""The 10th Kingdom"" parentTitle=""Season 1"" contentRating=""TV-PG"" summary=""The Trolls, now teamed up with the Huntsman, have captured Virginia, Tony and Prince. Meanwhile, The Evil Queen prepares the false Wendell&#39;s coronation. Tony and Virginia manage to escape, but Prince is left behind. On their way to Wendell&#8217;s castle to stop the coronation, they decide to take a shortcut through the Deadly Swamp. In the swamp, Virginia finds the body of  &#34;The Swamp Witch&#34; (Snow White&#39;s evil stepmother.) Tony eats some magic singing mushrooms and drinks the swamp water, and Virginia joins him. The mushrooms and swamp water make them fall into a dream filled sleep and swamp vines begin to cover them."" index=""9"" parentIndex=""1"" viewOffset=""237594"" lastViewedAt=""1690340556"" year=""2000"" thumb=""/library/metadata/8411/thumb/1711464915"" art=""/library/metadata/8405/art/1669931876"" parentThumb=""/library/metadata/8406/thumb/1711464916"" grandparentThumb=""/library/metadata/8405/thumb/1669931876"" grandparentArt=""/library/metadata/8405/art/1669931876"" grandparentTheme=""/library/metadata/8405/theme/1669931876"" duration=""5290004"" originallyAvailableAt=""2000-03-26"" addedAt=""1669931876"" updatedAt=""1711464915"" chapterSource=""media"">
<Media id=""14336"" duration=""5290004"" bitrate=""4180"" width=""1920"" height=""1080"" aspectRatio=""1.78"" audioChannels=""2"" audioCodec=""aac"" videoCodec=""hevc"" videoResolution=""1080"" container=""mkv"" videoFrameRate=""24p"" audioProfile=""lc"" videoProfile=""main 10"">
<Part id=""14337"" key=""/library/parts/14337/1548421685/file.mkv"" duration=""5290004"" file=""/home/swuser/floatingcloud/TV Shows/The 10th Kingdom/Season 1/The 10th Kingdom - S01E09-10 - Part 9 + Part 10.mkv"" size=""2748491802"" audioProfile=""lc"" container=""mkv"" videoProfile=""main 10"" />
</Media>
</Video>
</MediaContainer>";
        public const string Content1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<MediaContainer size=""2"" allowSync=""0"" title1=""Plex Library"">
<Directory allowSync=""1"" art=""/:/resources/movie-fanart.jpg"" composite=""/library/sections/1/composite/1713383413"" filters=""1"" refreshing=""0"" thumb=""/:/resources/movie.png"" key=""1"" type=""movie"" title=""Movies"" agent=""com.plexapp.agents.imdb"" scanner=""Plex Movie Scanner"" language=""en"" uuid=""32670b35-b573-4a4c-96e2-86f973eb7cb7"" updatedAt=""1711464805"" createdAt=""1669930186"" scannedAt=""1713383413"" content=""1"" directory=""1"" contentChangedAt=""1768504"" hidden=""0"">
<Location id=""11"" path=""/home/swuser/floatingfish/NewMovies"" />
<Location id=""12"" path=""/home/swuser/floatingcloud/Movies"" />
<Location id=""13"" path=""/home/swuser/Videos"" />
<Location id=""14"" path=""/home/swuser/Downloads/radarr"" />
<Location id=""15"" path=""/mnt/storage/movies"" />
<Location id=""16"" path=""/mnt/floatinghulk/NewMovies"" />
</Directory>
<Directory allowSync=""1"" art=""/:/resources/show-fanart.jpg"" composite=""/library/sections/2/composite/1713385522"" filters=""1"" refreshing=""0"" thumb=""/:/resources/show.png"" key=""2"" type=""show"" title=""TV Shows"" agent=""com.plexapp.agents.thetvdb"" scanner=""Plex Series Scanner"" language=""en"" uuid=""ca22bbb6-386c-45e1-80bf-0c7ceb723f32"" updatedAt=""1711464870"" createdAt=""1669930350"" scannedAt=""1713385522"" content=""1"" directory=""1"" contentChangedAt=""1768492"" hidden=""0"">
<Location id=""17"" path=""/home/swuser/floatingfish/NewShows"" />
<Location id=""18"" path=""/home/swuser/TV"" />
<Location id=""19"" path=""/home/swuser/floatingcloud/TV Shows"" />
<Location id=""20"" path=""/mnt/floatinghulk/NewShows"" />
<Location id=""21"" path=""/mnt/storage/tv"" />
</Directory>
</MediaContainer>
";
    public static Mock<IHttpClientFactory> SetupMockHttpClientFactory(Mock<HttpMessageHandler> handlerMock)
    {
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test.com/")
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        return httpClientFactoryMock;
    }

    public static Mock<HttpMessageHandler> SetupMockHttpMessageHandler(string content, HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            })
            .Verifiable();

        return handlerMock;
    }
    public static void VerifyMockHttpMessageHandler(Mock<HttpMessageHandler> handlerMock, string expectedUri)
    {
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.AtMostOnce(), // Verifies that the method was called exactly once
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get  // We expect a GET request
                && req.RequestUri == new Uri(expectedUri) // To this exact URI
            ),
            It.IsAny<CancellationToken>()
        );
    }
}