using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ParseJsonTrelloToHtmlTable.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : Controller
    {
        public async Task<string> ReadAsStringAsync(IFormFile file)
        {
            var result = new StringBuilder();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(await reader.ReadLineAsync());
            }
            return result.ToString();
        }

        [HttpPost]
        public IActionResult Index(IFormFile arquivo)
        {
            var jsonAsString = await ReadAsStringAsync(arquivo);


            return View();
        }
    }
}
