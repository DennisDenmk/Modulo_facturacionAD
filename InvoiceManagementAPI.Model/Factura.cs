using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceManagementAPI.Model
{
    [Table("facturas")]
    public class Factura
    {
        [Key]
        [Column("fac_numero_factura")]
        [MaxLength(15)]
        public string FacNumeroFactura { get; set; }

        [Column("cli_cedula")]
        [MaxLength(10)]
        [Required]
        public string CliCedula { get; set; }

        [Column("est_ruc")]
        [MaxLength(13)]
        [Required]
        public string EstRuc { get; set; }

        // ✅ CORREGIDO: Solo pem_id (int), eliminamos pem_id_punto_emision
        [Column("pem_id")]
        [Required]
        public int PemId { get; set; }

        [Column("fac_fecha_emision")]
        [Required]
        public DateTime FacFechaEmision { get; set; } = DateTime.Now;

        [Column("tip_id_tipop")]
        [Required]
        public int TipIdTipop { get; set; }

        [Column("fac_subtotal_sin_iva")]
        [Required]
        public decimal FacSubtotalSinIva { get; set; } = 0;

        [Column("fac_subtotal_con_iva")]
        [Required]
        public decimal FacSubtotalConIva { get; set; } = 0;

        [Column("fac_iva")]
        [Required]
        public decimal FacIva { get; set; } = 0;

        [Column("fac_total")]
        [Required]
        public decimal FacTotal { get; set; } = 0;

        [Column("fac_estado_factura")]
        [Required]
        public bool FacEstadoFactura { get; set; }

        [Column("fac_fecha_impresion")]
        public DateTime? FacFechaImpresion { get; set; }

        [Column("fac_usuario_creacion")]
        [MaxLength(50)]
        [Required]
        public string FacUsuarioCreacion { get; set; }

        [Column("fac_usuario_modificacion")]
        [MaxLength(50)]
        public string FacUsuarioModificacion { get; set; }

        [Column("fac_fecha_creacion")]
        [Required]
        public DateTime FacFechaCreacion { get; set; } = DateTime.Now;

        [Column("fac_fecha_modificacion")]
        public DateTime? FacFechaModificacion { get; set; }

        // Relaciones
        [ForeignKey("CliCedula")]
        public virtual Cliente Cliente { get; set; }

        [ForeignKey("TipIdTipop")]
        public virtual TipoPago TipoPago { get; set; }

        [ForeignKey("PemId")]
        public virtual PuntoEmision PuntoEmision { get; set; }

        public virtual ICollection<DetalleFactura> DetallesFactura { get; set; }
    }
}
