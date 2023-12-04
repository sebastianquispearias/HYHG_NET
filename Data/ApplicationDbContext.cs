using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CORRECTO30NOV.Models;

namespace CORRECTO30NOV.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<CORRECTO30NOV.Models.Joke>? Joke { get; set; }
        public DbSet<DepositoCliente> DepositosClientes { get; set; }
        public DbSet<MovimientoBancario> MovimientosBancarios { get; set; }
    }

    public class DepositoCliente
    {
        public int Id { get; set; }
        public string NumeroTransaccion { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaDeposito { get; set; }
        // Otros campos relevantes
    }

    public class MovimientoBancario
    {
        public int Id { get; set; }
        public string NumeroTransaccion { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaMovimiento { get; set; }
        // Otros campos relevantes
    }

}
