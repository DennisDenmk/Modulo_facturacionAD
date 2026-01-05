using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization; // <--- 1. IMPORTANTE: Agrega esto

namespace InvoiceManagementAPI.Model
{
    // Tabla: detalle_facturas
    [Table("detalle_facturas")]
    public class DetalleFactura
    {
        [Key]
        [Column("det_id_detalle")]
        public int DetIdDetalle { get; set; }

        [Column("fac_numero_factura")]
        [MaxLength(15)]
        // Nota: Al crear, esto se llena automáticamente en el backend, no es necesario que sea requerido en el JSON
        public string FacNumeroFactura { get; set; }

        [Column("det_id_producto_externo")]
        public int? DetIdProductoExterno { get; set; }

        [Column("det_codigo_producto")]
        [MaxLength(50)]
        public string DetCodigoProducto { get; set; }

        [Column("det_nombre_producto")]
        [MaxLength(200)]
        public string DetNombreProducto { get; set; }

        [Column("det_cantidad")]
        public int? DetCantidad { get; set; }

        [Column("det_precio_unitario")]
        public decimal? DetPrecioUnitario { get; set; }

        [Column("det_graba_iva")]
        public bool? DetGrabaIva { get; set; }

        [Column("det_subtotal")]
        public decimal? DetSubtotal { get; set; }

        [Column("det_porcentaje_iva")]
        public decimal? DetPorcentajeIva { get; set; }

        [Column("det_valor_iva")]
        public decimal? DetValorIva { get; set; }

        [Column("det_total")]
        public decimal? DetTotal { get; set; }

        // Relaciones
        [ForeignKey("FacNumeroFactura")]
        [JsonIgnore] 
        public virtual Factura? Factura { get; set; } 
    }
}