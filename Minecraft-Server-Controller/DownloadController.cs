using Microsoft.AspNetCore.Mvc;

namespace Minecraft_Server_Controller
{
    [ApiController]
    [Route("api/download")]
    public class DownloadController : ControllerBase, IDisposable
    {
        Stream? Stream;

        [HttpGet]
        public IActionResult GetFile([FromQuery] string filePath, [FromQuery] string fileName)
        {
            if (!System.IO.File.Exists(filePath))
                return NotFound(fileName);

            Stream fileStream = System.IO.File.OpenRead(filePath);

            Console.WriteLine($"Downloading File : {fileName}");

            if (fileStream == null)
                return Redirect($"/Error");

            return File(fileStream, "application/octet-stream", fileName, true);
        }

        public void Dispose()
        {
            if (Stream != null)
                Stream.Dispose();

            GC.Collect(2, GCCollectionMode.Aggressive, true);
        }
    }
}
