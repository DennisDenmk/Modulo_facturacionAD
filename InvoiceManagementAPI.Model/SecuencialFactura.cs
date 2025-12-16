using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceManagementAPI.Model
{
    [Table("secuenciales_factura")]
    public class SecuencialFactura
    {
        [Key]
        [Column("sec_id_secuencial")]
        public int SecIdSecuencial { get; set; }

        // ✅ CORREGIDO: Solo pem_id (int)
        [Column("pem_id")]
        [Required]
        public int PemId { get; set; }

        [Column("sec_secuencial_actual")]
        [Required]
        public int SecSecuencialActual { get; set; } = 1;

        [Column("sec_fecha_actualizacion")]
        [Required]
        public DateTime SecFechaActualizacion { get; set; } = DateTime.Now;

        // Relaciones
        [ForeignKey("PemId")]
        public virtual PuntoEmision PuntoEmision { get; set; }
    }
}
