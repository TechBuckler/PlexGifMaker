using SubtitlesParser.Classes;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace PlexGifMaker.Data
{
    public class PlexService
    {
        private readonly ILogger<PlexService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private string? _baseUri;
        private string? _token;
        private readonly string subtitlePath = Path.Combine(Directory.GetCurrentDirectory(), "subtitles");

        public PlexService(IHttpClientFactory httpClientFactory, ILogger<PlexService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger; // Initialize the logger
            subtitlePath = Path.Combine(Directory.GetCurrentDirectory(), "subtitles")
                       ?? throw new InvalidOperationException("Subtitle path cannot be determined.");

        }
        public void SetConfiguration(string baseUri, string token)
        {
            if (!Uri.IsWellFormedUriString(baseUri, UriKind.Absolute))
            {
                throw new ArgumentException("Invalid base URI.", nameof(baseUri));
            }

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
        public string? ExtractMediaPartInfo(string xmlContent)
        {
            XDocument doc = XDocument.Parse(xmlContent);
            var videoNode = doc.Descendants("Video").FirstOrDefault();
            if (videoNode != null)
            {
                var partNode = videoNode.Descendants("Part").FirstOrDefault();
                if (partNode != null)
                {
                    string? container = partNode?.Attribute("container")?.Value;
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
            List<Subtitle?> subtitles = new();
            try
            {
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    string? part = ExtractMediaPartInfo(content);
                    string? key = ExtractStreamKey(content);
                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    var subtitleNodes = doc.SelectNodes("//Stream[@streamType='3']");

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

        private string? ExtractStreamKey(string xmlContent)
        {
            XDocument doc = XDocument.Parse(xmlContent);
            var videoNode = doc.Descendants("Video").FirstOrDefault();
            if (videoNode != null)
            {
                var partNode = videoNode.Descendants("Part").FirstOrDefault();
                if (partNode != null)
                {
                    string? key = partNode?.Attribute("key")?.Value;
                    return key;
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
                    File.WriteAllText(Path.Combine(subtitlePath, "subtitle.srt"), content);
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
            if (string.IsNullOrEmpty(subtitleKey))
            {
                subtitleKey = selectedKey;
            }

            string videoFile = $"{_baseUri}{key}?X-Plex-Token={_token}";

            var outputPath = await GenerateGifAsync(videoFile, subtitleKey, startTime, duration, episodeId);
            return outputPath?.Replace("wwwroot", string.Empty);
        }

        public async Task DeleteSubtitleFilesAsync(string fileName = "")
        {
            var files = Directory.GetFiles(subtitlePath);
            if (fileName == "")
            {
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.LogInformation("Deleted old subtitle file: {file}", file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Failed to delete subtitle file {file}: {ex.Message}", file, ex.Message);
                        }
                    }
                });
            }
            else
            {
                File.Delete(Path.Combine(subtitlePath, fileName));
            }
        }

        private async Task<string?> GenerateGifAsync(string videoFile, string subtitleKey, TimeSpan startTime, TimeSpan duration, string episodeId)
        {
            var formattedStartTime = startTime.ToString(@"hh\hmm\mss\sfff\ms").Replace(":", "");
            var index = 0;
            if (int.TryParse(subtitleKey, out int i))
            {
                index = i;
            }
            var formattedEndTime = (startTime + duration).ToString(@"hh\hmm\mss\sfff\ms").Replace(":", "");
            var outputPath = Path.Combine("wwwroot", "gifs", $"{episodeId}_{formattedStartTime}_to_{formattedEndTime}.gif");
            outputPath = EnsureUniqueFilename(outputPath);
            string filters;
            var srt = Path.Combine(subtitlePath, "subtitle.srt");
            var sup = Path.Combine(subtitlePath, "subtitle.sup");
            if (!File.Exists(sup))
            {
                filters = $"fps=20,scale=400:-1:flags=lanczos,subtitles='{srt.Replace("\\", "\\\\")}':force_style='Fontsize=24'[v]";
            }
            else
            {
                string subtitleStream = $"[0:v][0:s:{index}]overlay[v]";
                filters = $"fps=20,scale=400:-1:flags=lanczos,{subtitleStream}";
            }

            var ffmpegCommand = $"-report -v debug -y -i \"{videoFile}\" -ss {startTime} -t {duration} -r 10 -lavfi \"{filters}\" -map \"[v]\" -c:v gif \"{outputPath}\"";
            _logger.LogInformation("Executing FFmpeg command: {FfmpegCommand}", ffmpegCommand);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments = ffmpegCommand;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                StringBuilder output = new();
                StringBuilder error = new();

                process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);
                process.ErrorDataReceived += (sender, args) => error.AppendLine(args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg encountered an error: {ErrorOutput}", error.ToString());
                    return null;
                }
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

        public async Task<List<SubtitleItem>> ExtractSubtitles(string episodeId, int index)
        {
            string metadataUrl = $"{_baseUri}/library/metadata/{episodeId}?X-Plex-Token={_token}";
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(metadataUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch video metadata for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                return new List<SubtitleItem>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = new XmlDocument();
            doc.LoadXml(content);
            var keyNode = doc.SelectSingleNode("(//Part)");
            var key = keyNode?.Attributes?["key"]?.Value;

            if (string.IsNullOrEmpty(key))
            {
                _logger.LogError("Failed to locate video file key in metadata for episode {EpisodeId}.", episodeId);
                return new List<SubtitleItem>();
            }

            List<SubtitleItem> items = new();
            string remoteUrl = key;
            string movieTitle = episodeId.Replace(' ', '_').Replace(":", "");
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "subtitles");
            Directory.CreateDirectory(outputDir);

            string outputFilenameSrt = $"subtitle.srt";
            string outputFilePathSrt = Path.Combine(outputDir, outputFilenameSrt);

            string ffmpegCommandSrt = $"ffmpeg -y -i \"{_baseUri}{remoteUrl}?X-Plex-Token={_token}\" -vcodec copy -map 0:s:{index} -c copy -an \"{outputFilePathSrt}\"";

            try
            {
                ExecuteCommand(ffmpegCommandSrt);
                Console.WriteLine($"Subtitles extracted successfully for {movieTitle}.");
            }
            catch (Exception e)
            {
                _logger.LogError("Error extracting subtitles as .srt: {e.Message}", e.Message);
                // Attempt to extract as .sup
                // Delete the failed .srt file
                await DeleteSubtitleFilesAsync("subtitle.srt");
                string outputFilenameSup = $"subtitle.sup";
                string outputFilePathSup = Path.Combine(outputDir, outputFilenameSup);
                string ffmpegCommandSup = $"ffmpeg -y -i \"{_baseUri}{remoteUrl}?X-Plex-Token={_token}\" -map 0:s:{index} -c copy \"{outputFilePathSup}\"";

                try
                {
                    ExecuteCommand(ffmpegCommandSup);
                    Console.WriteLine($"Subtitles extracted successfully as .sup for {movieTitle}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error extracting subtitles as .sup: {ex.Message}", ex.Message);
                }
                if (File.Exists(outputFilePathSup))
                {
                    // Parsing .sup files is not supported directly, you might need to handle this case accordingly.
                    // For example, you can log a message indicating that parsing .sup files is not supported.
                    _logger.LogWarning("Subtitle extraction as .srt failed, and parsing .sup files is not supported directly.");
                }
            }

            // Parsing the subtitle file
            if (File.Exists(outputFilePathSrt))
            {
                using StreamReader sr = new(outputFilePathSrt);
                var content2 = await sr.ReadToEndAsync();
                var parser = new SubtitlesParser.Classes.Parsers.SubParser();
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content2));
                items = parser.ParseStream(stream);
            }

            return items;
        }

        private static void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception("Command execution failed with non-zero exit code.");
                }
            }
        }

        // Define Subtitle and SubtitleCodec types if not already defined
        public class Subtitle
        {
            public string? Id { get; set; }
            public string? Language { get; set; }
            public string? Key { get; set; }
            public string? DisplayTitle { get; set; }
            public SubtitleCodec Codec { get; set; }
        }

        public enum SubtitleCodec
        {
            Unknown,
            SRT,
            ASS,
            // Add more codecs as needed
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
    public class SubtitleText
    {
        public string? Text { get; set; }
        // Additional attributes as needed
    }
}
