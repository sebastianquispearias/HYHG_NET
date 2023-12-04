using CORRECTO30NOV.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging; // Asegúrate de tener esta importación para ILogger

namespace CORRECTO30NOV.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult UploadPdf()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                ProcesarArchivoPdf(filePath);
                return RedirectToAction("Index");
            }

            return View();
        }

        private void ProcesarArchivoPdf(string filePath)
        {
            using (PdfReader reader = new PdfReader(filePath))
            {
                PdfDocument pdfDoc = new PdfDocument(reader);
                string text = "";
                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    text += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                }

                var datosVoucher = ExtraerDatosVoucher(text);
                // Aquí puedes hacer algo con los datos extraídos
            }
        }

        private VoucherData ExtraerDatosVoucher(string textoPdf)
        {
            var voucherData = new VoucherData();

            voucherData.Fecha = Regex.Match(textoPdf, @"Fecha:\s*(.*)\s*Hora:").Groups[1].Value.Trim();
            voucherData.CuentaCargo = Regex.Match(textoPdf, @"Cuenta de cargo:\s*(.*)").Groups[1].Value.Trim();
            voucherData.Empresa = Regex.Match(textoPdf, @"Empresa:\s*(.*)").Groups[1].Value.Trim();
            voucherData.CodigoOperacion = Regex.Match(textoPdf, @"Código de operación :\s*(\d+)").Groups[1].Value.Trim();
            voucherData.Monto = Regex.Match(textoPdf, @"Monto :\s*S/\s*(\d+,\d+\.\d+)").Groups[1].Value.Trim();

            return voucherData;
        }
    }

    public class VoucherData
    {
        public string Fecha { get; set; }
        public string CuentaCargo { get; set; }
        public string Empresa { get; set; }
        public string CodigoOperacion { get; set; }
        public string Monto { get; set; }
    }
}
