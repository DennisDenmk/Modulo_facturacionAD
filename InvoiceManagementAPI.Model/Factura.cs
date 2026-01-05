using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // <--- OBLIGATORIO

namespace InvoiceManagementAPI.Model
{
    [Table("facturas")]
    public class Factura
    {
        [Key]
        [Column("fac_numero_factura")]
        [MaxLength(15)]
        public string FacNumeroFactura { get; set; }

        // --- CLAVES FORÁNEAS (Estas SÍ son Required) ---
        [Column("cli_cedula")]
        [MaxLength(10)]
        [Required]
        public string CliCedula { get; set; }

        [Column("est_ruc")]
        [MaxLength(13)]
        [Required]
        public string EstRuc { get; set; }

        [Column("pem_id")]
        [Required]
        public int PemId { get; set; }

        [Column("tip_id_tipop")]
        [Required]
        public int TipIdTipop { get; set; }
        // ------------------------------------------------

        [Column("fac_fecha_emision")]
        [Required]
        public DateTime FacFechaEmision { get; set; } = DateTime.Now;

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

        // --- CAMPOS DE AUDITORÍA (Hazlos opcionales para evitar error 400) ---
        [Column("fac_usuario_creacion")]
        [MaxLength(50)]
        // [Required] <--- QUITA ESTO SI NO LO ENVÍAS DESDE EL FRONT
        public string? FacUsuarioCreacion { get; set; } // Agregado '?'

        [Column("fac_usuario_modificacion")]
        [MaxLength(50)]
        public string? FacUsuarioModificacion { get; set; } // Agregado '?'

        [Column("fac_fecha_creacion")]
        [Required]
        public DateTime FacFechaCreacion { get; set; } = DateTime.Now;

        [Column("fac_fecha_modificacion")]
        public DateTime? FacFechaModificacion { get; set; }

        // ==========================================
        // RELACIONES (Aquí estaba el conflicto principal)
        // ==========================================

        [ForeignKey("CliCedula")]
        [JsonIgnore] 
        public virtual Cliente? Cliente { get; set; } // <--- AGREGAR '?'

        [ForeignKey("TipIdTipop")]
        [JsonIgnore] 
        public virtual TipoPago? TipoPago { get; set; } // <--- AGREGAR '?'

        [ForeignKey("PemId")]
        [JsonIgnore] 
        public virtual PuntoEmision? PuntoEmision { get; set; } // <--- AGREGAR '?'

        public virtual ICollection<DetalleFactura> DetallesFactura { get; set; }
    }
}