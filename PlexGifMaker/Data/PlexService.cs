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
        private readonly string subtitlePath;

        public PlexService(IHttpClientFactory httpClientFactory, ILogger<PlexService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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

        public async Task<List<Library>> GetLibrariesAsync()
        {
            var libraries = new List<Library>();
            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}/library/sections?X-Plex-Token={_token}";

            try
            {
                var response = await client.GetAsync(requestUri);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt. Token may be invalid or expired.");
                    throw new UnauthorizedAccessException("Unable to access Plex libraries. Your session may have expired or you may not have the necessary permissions.");
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var doc = new XmlDocument();
                doc.LoadXml(content);

                var directoryNodes = doc.SelectNodes("//Directory");
                if (directoryNodes != null)
                {
                    foreach (XmlNode node in directoryNodes)
                    {
                        var id = node.Attributes?["key"]?.Value ?? string.Empty;
                        var title = node.Attributes?["title"]?.Value ?? "Untitled";
                        var type = node.Attributes?["type"]?.Value ?? "Unknown";

                        libraries.Add(new Library
                        {
                            Id = id,
                            Title = title,
                            Type = type
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("No library directories found in Plex response.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while fetching libraries");
                throw;
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "XML parsing error while processing Plex response");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching libraries");
                throw;
            }

            return libraries;
        }

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
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for library {LibraryKey}. Token may be invalid or user may lack permissions.", libraryKey);
                    throw new UnauthorizedAccessException($"Unable to access library {libraryKey}. Your session may have expired or you may not have the necessary permissions.");
                }
                else
                {
                    _logger.LogError("Failed to fetch episodes from library {LibraryKey}. Status code: {StatusCode}", libraryKey, response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch episodes. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch episodes from library {LibraryKey}", libraryKey);
                throw;
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
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for episode {EpisodeId}. Token may be invalid or user may lack permissions.", episodeId);
                    throw new UnauthorizedAccessException($"Unable to access episode {episodeId}. Your session may have expired or you may not have the necessary permissions.");
                }
                else
                {
                    _logger.LogError("Failed to fetch metadata for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch episode metadata. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching subtitles for episode {EpisodeId}", episodeId);
                throw;
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
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for subtitles of episode {EpisodeId}. Token may be invalid or user may lack permissions.", episodeId);
                    throw new UnauthorizedAccessException($"Unable to access subtitles for episode {episodeId}. Your session may have expired or you may not have the necessary permissions.");
                }
                else
                {
                    _logger.LogError("Failed to fetch subtitles for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch subtitles. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching subtitles for episode {EpisodeId}", episodeId);
                throw;
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
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for episode {EpisodeId}. Token may be invalid or user may lack permissions.", episodeId);
                    throw new UnauthorizedAccessException($"Unable to access episode {episodeId}. Your session may have expired or you may not have the necessary permissions.");
                }
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
            if (outputPath != null)
            {
                // Convert the full path to a relative path
                var relativePath = outputPath.Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "");
                // Ensure the path uses forward slashes for web URLs
                return relativePath.Replace("\\", "/");
            }
            return null;
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

            var ffmpegCommand = $"-report -v debug -i \"{videoFile}\" -ss {startTime} -t {duration} -r 10 -lavfi \"{filters}\" -map \"[v]\" -c:v gif \"{outputPath}\"";
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

                // Check if the file was actually created
                if (!File.Exists(outputPath))
                {
                    _logger.LogError("FFmpeg did not create the output file: {OutputPath}", outputPath);
                    return null;
                }
            }
            return outputPath;
        }

        private static string EnsureUniqueFilename(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath) ?? "DefaultDirectory";
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            int counter = 1;

            while (File.Exists(originalPath))
            {
                string newFilename = $"{filenameWithoutExt}_{counter++}{extension}";
                originalPath = Path.Combine(directory, newFilename);
            }

            return originalPath;
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
                            var type = node.Attributes?["type"]?.Value;
                            if (id != null && title != null)
                            {
                                items.Add(new Episode
                                {
                                    Id = id,
                                    Title = title,
                                    IsMovie = type?.Equals("movie", StringComparison.OrdinalIgnoreCase) ?? false
                                });
                            }
                        }
                        _logger.LogInformation("Fetched {ItemCount} items from library {LibraryKey}", items.Count, libraryKey);
                    }
                    else
                    {
                        _logger.LogWarning("No directories found in library {LibraryKey}. Treating as a flat structure.", libraryKey);
                        items = await GetMoviesAsync(libraryKey, true);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for library {LibraryKey}. Token may be invalid or user may lack permissions.", libraryKey);
                    throw new UnauthorizedAccessException($"Unable to access library {libraryKey}. Your session may have expired or you may not have the necessary permissions.");
                }
                else
                {
                    _logger.LogError("Failed to fetch content from library {LibraryKey}. Status code: {StatusCode}", libraryKey, response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch library content. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch content from library {LibraryKey}", libraryKey);
                throw;
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
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for library {LibraryKey}. Token may be invalid or user may lack permissions.", libraryKey);
                    throw new UnauthorizedAccessException($"Unable to access library {libraryKey}. Your session may have expired or you may not have the necessary permissions.");
                }
                else
                {
                    _logger.LogError("Failed to fetch movies from library {LibraryKey}. Status code: {StatusCode}", libraryKey, response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch movies. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch movies from library {LibraryKey}", libraryKey);
                throw;
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
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt for episode {EpisodeId}. Token may be invalid or user may lack permissions.", episodeId);
                throw new UnauthorizedAccessException($"Unable to access episode {episodeId}. Your session may have expired or you may not have the necessary permissions.");
            }
            else
            {
                _logger.LogError("Failed to fetch metadata for episode {EpisodeId}. Response status: {StatusCode}.", episodeId, response.StatusCode);
                throw new HttpRequestException($"Failed to fetch episode metadata. Status code: {response.StatusCode}");
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
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access attempt for episode {EpisodeId}. Token may be invalid or user may lack permissions.", episodeId);
                    throw new UnauthorizedAccessException($"Unable to access episode {episodeId}. Your session may have expired or you may not have the necessary permissions.");
                }
                _logger.LogError("Failed to fetch video metadata for episode {EpisodeId}. Status code: {StatusCode}", episodeId, response.StatusCode);
                throw new HttpRequestException($"Failed to fetch video metadata. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = new XmlDocument();
            doc.LoadXml(content);
            var keyNode = doc.SelectSingleNode("(//Part)");
            var key = keyNode?.Attributes?["key"]?.Value;

            if (string.IsNullOrEmpty(key))
            {
                _logger.LogError("Failed to locate video file key in metadata for episode {EpisodeId}.", episodeId);
                throw new InvalidOperationException($"Failed to locate video file key for episode {episodeId}.");
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
                _logger.LogError(e, "Error extracting subtitles as .srt: {ErrorMessage}", e.Message);
                // Attempt to extract as .sup
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
                    _logger.LogError(ex, "Error extracting subtitles as .sup: {ErrorMessage}", ex.Message);
                    throw;
                }
                if (File.Exists(outputFilePathSup))
                {
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

        public void RefreshToken()
        {
            _logger.LogInformation("Attempting to refresh Plex token...");
            throw new NotImplementedException("Token refresh not implemented. Please re-authenticate manually.");
        }

        public async Task<bool> CheckLibraryAccessAsync(string libraryKey)
        {
            var client = _httpClientFactory.CreateClient();
            var requestUri = $"{_baseUri}/library/sections/{libraryKey}?X-Plex-Token={_token}";

            try
            {
                var response = await client.GetAsync(requestUri);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error checking access to library {LibraryKey}", libraryKey);
                return false;
            }
        }
    }

    public class Library
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }
        public bool IsMovie { get; set; }
    }

    public class Episode
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public bool IsMovie { get; set; }
    }

    public class Subtitle
    {
        public string? Id { get; set; }
        public string? Language { get; set; }
        public string? Key { get; set; }
        public string? DisplayTitle { get; set; }
    }

    public enum SubtitleCodec
    {
        Unknown,
        SRT,
        ASS,
        // Add more codecs as needed
    }
}