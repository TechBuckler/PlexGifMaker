using SubtitlesParser.Classes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UtfUnknown;
using System.IO;
using static PlexGifMaker.Data.PlexService;
using MediaInfo;
using MediaInfo.Model;
namespace PlexGifMaker.Data
{
    public class PlexService
    {
        private readonly ILogger<PlexService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private string? _baseUri;
        private string? _token;

        public PlexService(IHttpClientFactory httpClientFactory, ILogger<PlexService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger; // Initialize the logger
        }
        public void SetConfiguration(string baseUri, string token)
        {
            if (!Uri.IsWellFormedUriString(baseUri, UriKind.Absolute))
            {
                throw new ArgumentException("Invalid base URI.", nameof(baseUri));
            }

            //if (string.IsNullOrWhiteSpace(token))
            //{
            //    throw new ArgumentException("Token cannot be empty.", nameof(token));
            //}

            _baseUri = baseUri;
            _token = token;
        }

        // Fetches the list of episodes from a specific show or library
        public async Task<List<Episode>> GetEpisodesAsync(string libraryKey, bool IsMovie)
        {
            var episodes = new List<Episode>();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var requestUri = $"{_baseUri}/library/metadata/{libraryKey}/allLeaves?X-Plex-Token={_token}";
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    var videoNodes = doc.SelectNodes("//Video");
                    if (videoNodes != null && videoNodes.Count > 0)
                    {
                        foreach (XmlNode node in videoNodes)
                        {
                            episodes.Add(new Episode
                            {
                                Id = node.Attributes?["ratingKey"]?.Value,
                                Title = node.Attributes?["title"]?.Value,
                                IsMovie = IsMovie
                            });
                        }
                        _logger.LogInformation("Fetched {EpisodeCount} episodes from library {LibraryKey}", episodes.Count, libraryKey);
                    }
                    else
                    {
                        _logger.LogWarning("No episodes found in library {LibraryKey}", libraryKey);
                    }
                }
                else
                {
                    _logger.LogError("Failed to fetch episodes from library {LibraryKey}. Status code: {StatusCode}", libraryKey, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch episodes from library {LibraryKey}. Error Message: {ErrorMessage}", libraryKey, ex.Message);
            }

            return episodes;
        }
        public string ExtractMediaPartInfo(string xmlContent)
        {
            XDocument doc = XDocument.Parse(xmlContent);
            var videoNode = doc.Descendants("Video").FirstOrDefault();
            if (videoNode != null)
            {
                var partNode = videoNode.Descendants("Part").FirstOrDefault();
                if (partNode != null)
                {
                    string container = partNode?.Attribute("container")?.Value;
                    return container;
                }
                else
                {
                    _logger.LogInformation("No Part element found.");
                }
            }
            else
            {
                _logger.LogInformation("No Video element found.");
            }
            return null;
        }
        public async Task<List<Subtitle?>> GetSubtitleOptionsAsync(string episodeId)
        {
            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}/library/metadata/{episodeId}?X-Plex-Token={_token}";
            List<Subtitle?> subtitles = new List<Subtitle?>();
            try
            {
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    string part = ExtractMediaPartInfo(content);
                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    var subtitleNodes = doc.SelectNodes("//Stream[@streamType='3']");

                    if(part == "mkv")
                    {
                        var embeddedSubtitles = GetEmbeddedSubtitles($"{_baseUri}/library/metadata/{episodeId}/file?X-Plex-Token={_token}");
                        foreach(var subtitle in embeddedSubtitles)
                        {
                            subtitles.Add(subtitle);
                        }
                    }

                    if (subtitleNodes != null)
                    {

                        foreach (XmlNode node in subtitleNodes)
                        {
                            var subtitle = new Subtitle
                            {
                                Id = node.Attributes?["id"]?.Value ?? string.Empty,
                                Language = node.Attributes?["language"]?.Value ?? "Unknown",
                                Key = node.Attributes?["key"]?.Value ?? string.Empty,
                                DisplayTitle = node.Attributes?["displayTitle"]?.Value ?? "Unknown"
                            };
                            subtitles.Add(subtitle);
                        }                        
                    }
                    else
                    {
                        _logger.LogInformation("No English subtitles found for episode {EpisodeId}.", episodeId);
                    }
                }
                else
                {
                    _logger.LogError("Failed to fetch metadata for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching subtitles for episode {EpisodeId}: {ErrorMessage}", episodeId, ex.Message);
            }
            return subtitles;
        }
        public async Task<List<SubtitleItem>> GetActualSubtitlesAsync(string episodeId, string key)
        {
            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}{key}?X-Plex-Token={_token}";

            try
            {
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var parser = new SubtitlesParser.Classes.Parsers.SubParser();
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                    var items = parser.ParseStream(stream);

                    if (items == null)
                    {
                        _logger.LogWarning("No subtitles were parsed for episode {EpisodeId}.", episodeId);
                        return new List<SubtitleItem>();
                    }

                    return items;
                }
                else
                {
                    _logger.LogError("Failed to fetch subtitles for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                    return new List<SubtitleItem>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching subtitles for episode {EpisodeId}: {ErrorMessage}", episodeId, ex.Message);
                return new List<SubtitleItem>();
            }
        }
        public async Task<string?> CreateGifFromSubtitlesAsync(string episodeId, int startSubtitleTime, int endSubtitleTime, string selectedKey = "")
        {
            var startTime = TimeSpan.FromMilliseconds(startSubtitleTime);
            var endTime = TimeSpan.FromMilliseconds(endSubtitleTime);
            var duration = endTime - startTime;

            if (duration <= TimeSpan.Zero)
            {
                _logger.LogError("End time {EndTime} must be greater than start time {StartTime}.", endTime, startTime);
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_baseUri}/library/metadata/{episodeId}?X-Plex-Token={_token}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch video metadata for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = new XmlDocument();
            doc.LoadXml(content);

            var keyNode = doc.SelectSingleNode("(//Part)");
            var key = keyNode?.Attributes?["key"]?.Value;
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogError("Failed to locate video file key in metadata for episode {EpisodeId}.", episodeId);
                return null;
            }

            var subtitleKeyNode = doc.SelectSingleNode("//Stream[@streamType='3' and (@languageCode='eng' or @language='English')]");
            var subtitleKey = subtitleKeyNode?.Attributes?["key"]?.Value;
            if(string.IsNullOrEmpty(subtitleKey))
            {
                subtitleKey = selectedKey;
            }

            string videoFile = $"{_baseUri}{key}?X-Plex-Token={_token}";
            string localSubtitlePath = !string.IsNullOrEmpty(subtitleKey)
                ? await DownloadSubtitleAsync(client, $"{_baseUri}{subtitleKey}?X-Plex-Token={_token}")
                : string.Empty;

            if (string.IsNullOrEmpty(localSubtitlePath) && !string.IsNullOrEmpty(subtitleKey))
            {
                _logger.LogWarning("Subtitle download failed for episode {EpisodeId}. Proceeding without subtitles.", episodeId);
            }

            var outputPath = await GenerateGifAsync(videoFile, localSubtitlePath, startTime, duration, episodeId);
            return outputPath?.Replace("wwwroot", string.Empty);
        }
        private async Task<string> DownloadSubtitleAsync(HttpClient client, string subtitleUrl)
        {
            string directoryPath = Path.Combine("wwwroot", "gifs");

            // Ensure the directory exists
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var localSubtitlePath = Path.Combine(directoryPath, "subtitle.srt");
            var subtitleResponse = await client.GetAsync(subtitleUrl);
            if (subtitleResponse.IsSuccessStatusCode)
            {
                var subtitleBytes = await subtitleResponse.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(localSubtitlePath, subtitleBytes);
                _logger.LogInformation("Subtitles downloaded successfully to {localSubtitlePath}", localSubtitlePath);
                

                // Detect encoding and convert to UTF-8 if necessary
                var detectedResult = CharsetDetector.DetectFromBytes(subtitleBytes);
                if (detectedResult.Detected?.EncodingName !=  "utf-8")
                {
                    var subtitleText = BitConverter.ToString(subtitleBytes);
                    await File.WriteAllTextAsync(localSubtitlePath, subtitleText, Encoding.UTF8);
                }

                _logger.LogInformation("Subtitles downloaded successfully.");
            }
            else
            {
                _logger.LogWarning("Failed to download subtitles from {SubtitleUrl}.", subtitleUrl);
            }
            return localSubtitlePath;
        }

        private async Task<string?> GenerateGifAsync(string videoFile, string subtitlePath, TimeSpan startTime, TimeSpan duration, string episodeId)
        {
            var formattedStartTime = startTime.ToString(@"hh\hmm\mss\sfff\ms").Replace(":", "");
            var formattedEndTime = (startTime + duration).ToString(@"hh\hmm\mss\sfff\ms").Replace(":", "");
            var outputPath = Path.Combine("wwwroot", "gifs", $"{episodeId}_{formattedStartTime}_to_{formattedEndTime}.gif");
            outputPath = EnsureUniqueFilename(outputPath);

            var filters = string.IsNullOrEmpty(subtitlePath) ? "fps=20,scale=400:-1:flags=lanczos" : $"fps=20,scale=400:-1:flags=lanczos,subtitles='{subtitlePath}':force_style='Fontsize=24'";
            var ffmpegCommand = $"-report -v debug -i \"{videoFile}\" -ss {startTime} -t {duration} -r 10 -lavfi \"{filters}\" \"{outputPath}\"";
            _logger.LogInformation("Executing FFmpeg command: {FfmpegCommand}", ffmpegCommand);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments = ffmpegCommand;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg encountered an error.");
                    return null;
                }
            }

            if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
            {
                File.Delete(subtitlePath);
            }

            return outputPath;
        }
        private static string EnsureUniqueFilename(string originalPath)
        {
            // Get the directory name and ensure it's not null or empty.
            // If it is, set a default directory or handle as needed.
            string directory = Path.GetDirectoryName(originalPath) ?? "DefaultDirectory"; // Adjust "DefaultDirectory" as needed.
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            int counter = 1;

            // Construct a new path with incremented filenames until a unique name is found.
            while (File.Exists(originalPath))
            {
                string newFilename = $"{filenameWithoutExt}_{counter++}{extension}";
                originalPath = Path.Combine(directory, newFilename);
            }

            return originalPath;
        }
        public async Task<List<Library>> GetLibrariesAsync()
        {
            var libraries = new List<Library>();
            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}/library/sections?X-Plex-Token={_token}";

            try
            {
                var response = await client.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var doc = new XmlDocument();
                    doc.LoadXml(content);
                    if (doc is not null)
                    {
                        var videoNodes = doc.SelectNodes("//Directory");
                        if (videoNodes is not null)
                        {
                            foreach (XmlNode node in videoNodes)
                            {
                                var id = node.Attributes?["key"]?.Value ?? string.Empty; // Fallback to empty string if null
                                var title = node.Attributes?["title"]?.Value ?? "Untitled"; // Fallback to "Untitled" if null
                                var type = node.Attributes?["type"]?.Value ?? "Unknown"; // Fallback to "Unknown" if null

                                var library = new Library
                                {
                                    Id = id,
                                    Title = title,
                                    Type = type
                                };

                                libraries.Add(library);
                            }
                        }
                    }
                }
                else
                {
                    // Log the error or handle it according to your error policy
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching libraries.");
            }

            return libraries;
        }
        public async Task<List<Episode>> GetShowsAsync(string libraryKey)
        {
            var items = new List<Episode>();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var requestUri = $"{_baseUri}/library/sections/{libraryKey}/all?X-Plex-Token={_token}";
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    var directoryNodes = doc.SelectNodes("//Directory");
                    if (directoryNodes != null && directoryNodes.Count > 0)
                    {
                        foreach (XmlNode node in directoryNodes)
                        {
                            var id = node.Attributes?["ratingKey"]?.Value;
                            var title = node.Attributes?["title"]?.Value;

                            if (id != null && title != null)
                            {
                                items.Add(new Episode
                                {
                                    Id = id,
                                    Title = title,
                                });
                            }
                        }
                        _logger.LogInformation("Fetched {ItemCount} shows from library {LibraryKey}", items.Count, libraryKey);
                    }
                    else
                    {
                        _logger.LogWarning("No shows (directories) found in library {LibraryKey}. Treating as a flat structure.", libraryKey);
                        return await GetMoviesAsync(libraryKey, true);
                    }
                }
                else
                {
                    _logger.LogError("Failed to fetch content from library {LibraryKey}. Status code: {StatusCode}", libraryKey, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch content from library {LibraryKey}. Error Message: {ErrorMessage}", libraryKey, ex.Message);
            }

            return items;
        }
        public async Task<List<Episode>> GetMoviesAsync(string libraryKey, bool IsMovie)
        {
            var episodes = new List<Episode>();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var requestUri = $"{_baseUri}/library/sections/{libraryKey}/all?X-Plex-Token={_token}";
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    var videoNodes = doc.SelectNodes("//Video");
                    if (videoNodes != null)
                    {
                        foreach (XmlNode node in videoNodes)
                        {
                            var id = node?.Attributes?["ratingKey"]?.Value;
                            var title = node?.Attributes?["title"]?.Value;
                            if (id != null && title != null)
                            {
                                episodes.Add(new Episode
                                {
                                    Id = id,
                                    Title = title,
                                    IsMovie = IsMovie
                                });
                            }
                        }
                    }

                    if (episodes.Count == 0)
                    {
                        _logger.LogWarning("No movies found in library {LibraryKey}.", libraryKey);
                        var directoryNodes = doc.SelectNodes("//Directory");
                        if (directoryNodes != null)
                        {
                            foreach (XmlNode node in directoryNodes)
                            {
                                var id = node?.Attributes?["ratingKey"]?.Value;
                                var title = node?.Attributes?["title"]?.Value;
                                if (id != null && title != null)
                                {
                                    episodes.Add(new Episode
                                    {
                                        Id = id,
                                        Title = title,
                                        IsMovie = IsMovie
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Fetched {EpisodeCount} movies from library {LibraryKey}.", episodes.Count, libraryKey);
                    }
                }
                else
                {
                    _logger.LogError("Failed to fetch movies from library {LibraryKey}. Status code: {StatusCode}", libraryKey, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch movies from library {LibraryKey}. Exception: {Exception}", libraryKey, ex);
            }

            return episodes;
        }
        public async Task<TimeSpan> GetEpisodeDurationAsync(string episodeId)
        {
            if (string.IsNullOrEmpty(episodeId))
            {
                throw new ArgumentException("Episode ID cannot be null or empty.", nameof(episodeId));
            }

            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}/library/metadata/{episodeId}?X-Plex-Token={_token}";
            var response = await client.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = new XmlDocument();
                doc.LoadXml(content);

                // Extract the duration from the episode metadata. Assuming duration is provided in milliseconds.
                var node = doc.SelectSingleNode("//Video");
                if (node?.Attributes?["duration"] != null)
                {
                    if (int.TryParse(node?.Attributes?["duration"]?.Value, out int durationInMilliseconds))
                    {
                        return TimeSpan.FromMilliseconds(durationInMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse the duration for episode {EpisodeId}.", episodeId);
                    }
                }
                else
                {
                    _logger.LogWarning("Duration attribute is missing for episode {EpisodeId}.", episodeId);
                }
            }
            else
            {
                _logger.LogError("Failed to fetch metadata for episode {EpisodeId}. Response status: {StatusCode}.", episodeId, response.StatusCode);
            }

            return TimeSpan.Zero;
        }

        public async Task FetchSubtitlesAsync(string? episodeId)
        {
            if (string.IsNullOrEmpty(episodeId))
            {
                _logger.LogError("Episode ID is null or empty.");
                return;
            }
            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}/library/metadata/{episodeId}/subtitles?language=en&hearingImpaired=0&forced=0&X-Plex-Token={_token}"; //GET request
            try
            {
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var streamId = ExtractStreamIdFromXml(content);
                    if (streamId == null)
                    {
                        _logger.LogWarning("No subtitles were found from OpenSubtitles for episode {EpisodeId}.", episodeId);
                        return;
                    }
                    else
                    {
                        var items = new List<SubtitleItem>();
                        var requestSubUri = $"{_baseUri}/library/metadata/{episodeId}/subtitles?key=%2Flibrary%2Fstreams%2F{streamId}&codec=srt&language=eng&hearingImpaired=0&forced=0&providerTitle=OpenSubtitles&X-Plex-Token={_token}"; //PUT request
                        var responseSub = await client.PutAsync(requestSubUri, null);
                        if (responseSub.IsSuccessStatusCode)
                        {
                            var contentSub = await responseSub.Content.ReadAsStringAsync();
                            _logger.LogInformation("Subtitles fetched successfully for episode {EpisodeId}.", episodeId);
                            return;
                        }
                        else
                        {
                            _logger.LogError("Failed to fetch subtitles for episode {EpisodeId}. Status code: {StatusCode}", episodeId, responseSub.StatusCode);
                            return;
                        }
                    }
                }
                else
                {
                    _logger.LogError("Failed to fetch subtitles for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching subtitles for episode {EpisodeId}: {ErrorMessage}", episodeId, ex.Message);
                return;
            }
        }

        // Method to extract subtitles embedded within an MKV file
        public List<Subtitle> GetEmbeddedSubtitles(string filePath)
        {
            var subtitles = new List<Subtitle>();
            if (!File.Exists(filePath))
            {
                _logger.LogError("File does not exist: {FilePath}", filePath);
                return subtitles;
            }

            try
            {
                var mediaInfo = new MediaInfoWrapper(filePath);
                var subtitleTracks = mediaInfo.Subtitles;
                foreach (var track in subtitleTracks)
                {
                    if (!string.IsNullOrEmpty(track.Language) || !string.IsNullOrEmpty(track.Codec.ToString()))
                    {
                        subtitles.Add(new Subtitle
                        {
                            Language = track.Language,
                            Key = track.Id.ToString(),
                            DisplayTitle = $"{track.Language} - {track.Format}",
                            Codec = track.Codec
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract subtitles from file: {FilePath}", filePath);
            }

            return subtitles;
        }
    

    public int? ExtractStreamIdFromXml(string xmlContent)
        {
            try
            {
                // Load the XML content into an XDocument
                var document = XDocument.Parse(xmlContent);

                // Retrieve the first <Stream> element
                var firstStream = document.Descendants("Stream").FirstOrDefault();

                // Get the 'sourceKey' attribute value
                var sourceKey = firstStream?.Attribute("key")?.Value;

                if (sourceKey != null)
                {
                    // Extract the number after "/library/streams/"
                    var parts = sourceKey.Split('/');
                    if (parts.Length > 2)
                    {
                        // The ID is expected to be the last part of the 'sourceKey'
                        if (int.TryParse(parts.Last(), out int streamId))
                        {
                            return streamId; // Return the extracted ID
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return null; // Return -1 or any other indicator for failure or non-existence
        }
    }
    public class Library
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }  // Type of the library (movie, show, music, etc.)
    }
    public class Episode
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        // Additional attributes as needed
        public bool IsMovie { get; set; }
    }
    public class Subtitle
    {
        public string? Id { get; set; }
        public string? Language { get; set; }
        public string? Key { get; set; }
        public string? DisplayTitle { get; set; }
        public SubtitleCodec Codec { get; internal set; }
        // Additional attributes as needed
    }
    public class SubtitleText
    {
        public string? Text { get; set; }
        // Additional attributes as needed
    }
}
