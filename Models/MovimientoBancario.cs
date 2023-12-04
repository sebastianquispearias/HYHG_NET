namespace CORRECTO30NOV.Models
{
    public class MovimientoBancario
    {
        public int Id { get; set; }
        public string NumeroTransaccion { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaMovimiento { get; set; }
        // Otros campos relevantes
    }

}
