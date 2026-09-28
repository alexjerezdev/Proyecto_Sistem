using System;
using System.Collections.Generic;

namespace luisfrontend.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        
        // Estado: "Pendiente", "En Preparacion", "Entregado"
        public string Estado { get; set; } = "Pendiente";

        public List<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}