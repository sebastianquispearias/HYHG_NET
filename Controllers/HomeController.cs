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
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

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

                var datosVoucher = ProcesarArchivoPdf(filePath);
                var movimientosBancarios = ProcesarMovimientosBancarios();
                var coincidencia = CompararDatos(datosVoucher, movimientosBancarios);

                ViewBag.ResultadoComparacion = coincidencia ? "Coincidencia encontrada." : "No se encontraron coincidencias.";

                return View("Index");
            }

            return View();
        }

        private VoucherData ProcesarArchivoPdf(string filePath)
        {
            using (PdfReader reader = new PdfReader(filePath))
            {
                PdfDocument pdfDoc = new PdfDocument(reader);
                string text = "";
                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    text += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                }

                return ExtraerDatosVoucher(text);
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

        private List<MovimientoBancario> ProcesarMovimientosBancarios()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/bank/constanciamovimientos.pdf");
            var movimientosBancarios = new List<MovimientoBancario>();

            using (PdfReader reader = new PdfReader(filePath))
            {
                PdfDocument pdfDoc = new PdfDocument(reader);
                string text = "";
                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    text += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                }

                var lineas = text.Split('\n');
                foreach (var linea in lineas)
                {
                    if (linea.Contains("DEPOSITO CAJERO"))
                    {
                        var datos = linea.Split(' ');

                        // Verifica que 'datos' tenga suficientes elementos
                        if (datos.Length > 6 && decimal.TryParse(datos[6].Replace("US$", "").Replace("+", "").Trim(), out decimal montoParsed))
                        {
                            var movimiento = new MovimientoBancario
                            {
                                FechaMovimiento = DateTime.Parse(datos[3] + " " + datos[4]),
                                Monto = montoParsed
                            };
                            movimientosBancarios.Add(movimiento);
                        }
                        else
                        {
                            // Opcional: Manejar el caso en que no hay suficientes elementos o la conversión falla
                            // Por ejemplo, puedes registrar un error o continuar con el siguiente elemento
                        }
                    }
                }

                return movimientosBancarios;
            }
        }



        private bool CompararDatos(VoucherData datosVoucher, List<MovimientoBancario> movimientosBancarios)
        {
            var montoVoucher = decimal.Parse(datosVoucher.Monto.Replace("S/", "").Trim());
            return movimientosBancarios.Any(m => m.Monto == montoVoucher && m.FechaMovimiento.ToString("dd MMM yyyy") == datosVoucher.Fecha);
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

    public class MovimientoBancario
    {
        public DateTime FechaMovimiento { get; set; }
        public decimal Monto { get; set; }
    }
}
