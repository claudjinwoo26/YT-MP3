using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace MP3CONVERTER.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public HomeController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> DownloadMp3(string youtubeUrl)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl))
            {
                ViewBag.Error = "Please enter a valid YouTube URL.";
                return View("Index");
            }

            try
            {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(youtubeUrl);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (audioStreamInfo == null)
                {
                    ViewBag.Error = "No audio stream found.";
                    return View("Index");
                }

                string fileName = $"{video.Title}.mp3".Replace(" ", "_");
                string downloadsFolder = Path.Combine(_env.WebRootPath, "downloads");
                Directory.CreateDirectory(downloadsFolder);

                string audioFilePath = Path.Combine(downloadsFolder, $"{video.Title}.mp4");
                string mp3FilePath = Path.Combine(downloadsFolder, fileName);

                await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioFilePath);

                ConvertToMp3(audioFilePath, mp3FilePath);

                System.IO.File.Delete(audioFilePath);

                if (!System.IO.File.Exists(mp3FilePath))
                {
                    throw new Exception("MP3 conversion failed. File not found.");
                }

                Console.WriteLine($"Conversion completed. MP3 saved at: {mp3FilePath}");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(mp3FilePath);
                return File(fileBytes, "audio/mpeg", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ViewBag.Error = "An error occurred: " + ex.Message;
                return View("Index");
            }
        }

        private void ConvertToMp3(string inputFile, string outputFile)
        {
            string ffmpegPath;

            // Check if running on Windows or Linux
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ffmpegPath = _configuration["FFmpegPath:Windows"];
            }
            else
            {
                ffmpegPath = _configuration["FFmpegPath:Linux"];
            }

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new Exception("FFmpeg path is not configured in appsettings.json.");
            }

            Console.WriteLine($"Using FFmpeg at: {ffmpegPath}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputFile}\" -vn -b:a 192k \"{outputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }
    }
}
