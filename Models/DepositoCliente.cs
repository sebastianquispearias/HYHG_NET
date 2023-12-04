namespace CORRECTO30NOV.Models
{
    public class DepositoCliente
    {
        public int Id { get; set; }
        public string NumeroTransaccion { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaDeposito { get; set; }
        // Otros campos relevantes
    }

}
