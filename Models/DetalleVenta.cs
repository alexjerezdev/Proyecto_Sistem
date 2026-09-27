namespace luisfrontend.Models
{
    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public Venta? Venta { get; set; }

        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}