using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace ParseJsonTrelloToHtmlTable.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : Controller
    {
        private async Task<string> ReadAsStringAsync(IFormFile file)
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
        public async Task<IActionResult> Index(IFormFile arquivo)
        {
            var jsonAsString = await ReadAsStringAsync(arquivo);

            var model = JsonSerializer.Deserialize<TrelloDTO>(jsonAsString);

            var html = @"<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/font-awesome@4.7.0/css/font-awesome.css'>
                <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/css/bootstrap.min.css' integrity='sha384-rbsA2VBKQhggwzxH7pPCaAqO46MgnOM80zW1RWuH61DGLwZJEdK2Kadq2F9CUG65' crossorigin='anonymous'>
                <table class=table table-bordered><tbody>";

            int index = 1;
            foreach (var (listDTO, index1) in model.Lists.Select((TrelloDTO.ListDTO listDTO, int index) => (listDTO, index)))
            {
                var cards = model.Cards.Where(x => x.ListId == listDTO.Id).ToList();

                html += $@"<tr>
                    <td align='center'><b>{(index1 + 1)}</b></td>
                    <td align='center' colspan='3'><strong>{listDTO.Nome}</strong></td>
                 </tr>";

                foreach (var (card, index2) in cards.Select((TrelloDTO.CardDTO listDTO, int index) => (listDTO, index)))
                {
                    var checklists = model.Checklists.Where(x => card.ChecklistIds.Contains(x.Id)).ToList();

                    var checkListHtml = "";

                    if (checklists.Any())
                    {
                        checkListHtml = "<ol>";

                        foreach (var checklist in checklists)
                        {
                            checkListHtml += $"<li>{checklist.Nome}<ol>";
                            foreach (var item in checklist.Itens)
                            {
                                checkListHtml += $"<li><i class='fa {(item.Status == "complete" ? "fa-check text-success" : "fa-times text-danger")}'></i> - {item.Nome}</li>";
                            }
                            checkListHtml += "</ol></li>";

                            checkListHtml += "<ol>";
                        }
                    }

                    var labels = model.Labels.Where(x => card.LabelIds.Contains(x.Id)).ToList();

                    var labelsHtml = "";

                    if (labels.Any())
                    {
                        foreach (var label in labels)
                        {
                            if (!string.IsNullOrEmpty(labelsHtml))
                            {
                                labelsHtml += "<br>";
                            }

                            string corTexto = "";

                            switch (label.Cor)
                            {
                                case "yellow":
                                case "":
                                    corTexto = "black";
                                    break;
                                default:
                                    corTexto = "white";
                                    break;
                            }

                            labelsHtml += $"<span class='badge' style='background: {label.Cor}; color: {corTexto};'>{label.Nome}</span>";
                        }
                    }

                    html += $@"<tr>
                        <td align='center'>{(index2 + 1)}</td>
                        <td align='center'>{index++}</td>
                        <td>{(card.Arquivado ? "[Arquivado] " : "")}{card.Titulo}{(!string.IsNullOrEmpty(card.Descricao) ? "<br><code>" + card.Descricao + "</code>" : "")}<br>{checkListHtml}</td>
                        <td align='center'>{labelsHtml}</td>
                     </tr>";
                }
            }

            html += "</tbody></table>";

            return File(Encoding.UTF8.GetBytes(html), "text/html");
        }
    }

    public class TrelloDTO
    {
        [JsonPropertyName("cards")]
        public List<CardDTO> Cards { get; set; }

        [JsonPropertyName("lists")]
        public List<ListDTO> Lists { get; set; }

        [JsonPropertyName("labels")]
        public List<LabelDTO> Labels { get; set; }

        [JsonPropertyName("checklists")]
        public List<ChecklistDTO> Checklists { get; set; }

        public class ChecklistDTO
        {
            [JsonPropertyName("name")]
            public string Nome { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("checkItems")]
            public List<ChecklistItemDTO> Itens { get; set; }

            public class ChecklistItemDTO
            {
                [JsonPropertyName("name")]
                public string Nome { get; set; }

                [JsonPropertyName("state")]
                public string Status { get; set; }
            }
        }

        public class LabelDTO
        {
            [JsonPropertyName("name")]
            public string Nome { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("color")]
            public string Cor { get; set; }
        }

        public class ListDTO
        {
            [JsonPropertyName("name")]
            public string Nome { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public class CardDTO
        {
            private string _desc { get; set; }

            [JsonPropertyName("name")]
            public string Titulo { get; set; }

            [JsonPropertyName("desc")]
            public string Descricao
            {
                get
                {
                    return _desc.Replace("\n", "<br>");
                }
                set => _desc = value;
            }

            [JsonPropertyName("closed")]
            public bool Arquivado { get; set; }

            [JsonPropertyName("idList")]
            public string ListId { get; set; }

            [JsonPropertyName("idChecklists")]
            public List<string> ChecklistIds { get; set; }

            [JsonPropertyName("idLabels")]
            public List<string> LabelIds { get; set; }
        }
    }
}
