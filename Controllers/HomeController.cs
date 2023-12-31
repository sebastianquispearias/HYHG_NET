﻿using CORRECTO30NOV.Models;
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
using System.Globalization;

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
            bool procesarTransacciones = false;

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
                    if (linea.StartsWith("Cargo realizado por Fecha y hora Monto"))
                    {
                        procesarTransacciones = true;
                        continue;
                    }

                    if (procesarTransacciones)
                    {
                        var partes = linea.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        int indiceFecha = -1;
                        for (int i = 0; i < partes.Length; i++)
                        {
                            if (Regex.IsMatch(partes[i], @"\bene\b|\bfeb\b|\bmar\b|\babr\b|\bmay\b|\bjun\b|\bjul\b|\bago\b|\bsep\b|\boct\b|\bnov\b|\bdic\b"))
                            {
                                indiceFecha = i;
                                break;
                            }
                        }

                        if (indiceFecha != -1 && partes.Length > indiceFecha + 2)
                        {
                            try
                            {
                                var dia = int.Parse(partes[indiceFecha - 1]);
                                var mes = MesANumero(partes[indiceFecha]);
                                var año = DateTime.Now.Year; // Asumiendo el año actual
                                var horaMinutos = partes[indiceFecha + 1].Split(':');
                                var hora = int.Parse(horaMinutos[0]);
                                var minutos = int.Parse(horaMinutos[1]);

                                var fechaHora = new DateTime(año, mes, dia, hora, minutos, 0);
                                var montoStr = partes[partes.Length - 1].Replace("S/", "").Replace(",", "").Trim();
                                var montoParsed = decimal.Parse(montoStr);

                                var movimiento = new MovimientoBancario
                                {
                                    FechaMovimiento = fechaHora,
                                    Monto = montoParsed
                                };
                                movimientosBancarios.Add(movimiento);
                            }
                            catch (Exception)
                            {
                                // Manejo de errores de formato o conversión
                            }
                        }
                    }



                }

            }

            return movimientosBancarios;
        }
        private int MesANumero(string mes)
        {
            return mes switch
            {
                "ene" => 1,
                "feb" => 2,
                "mar" => 3,
                "abr" => 4,
                "may" => 5,
                "jun" => 6,
                "jul" => 7,
                "ago" => 8,
                "sep" => 9,
                "oct" => 10,
                "nov" => 11,
                "dic" => 12,
                _ => 0
            };
        }


        public class MovimientoBancario
        {
            public DateTime FechaMovimiento { get; set; }
            public decimal Monto { get; set; }
        }






        private bool CompararDatos(VoucherData datosVoucher, List<MovimientoBancario> movimientosBancarios)
        {
            var montoVoucher = decimal.Parse(datosVoucher.Monto.Replace("S/", "").Replace(",", "").Trim());

            // Ajustar la fecha para que coincida con la abreviatura correcta del mes
            var fechaAjustada = datosVoucher.Fecha.Replace("sep", "sept.");

            if (DateTime.TryParseExact(fechaAjustada, "dd MMM yyyy", new CultureInfo("es-ES"), DateTimeStyles.None, out DateTime fechaVoucher))
            {
                return movimientosBancarios.Any(m => m.Monto == montoVoucher && m.FechaMovimiento.Date == fechaVoucher.Date);
            }

            return false;
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
