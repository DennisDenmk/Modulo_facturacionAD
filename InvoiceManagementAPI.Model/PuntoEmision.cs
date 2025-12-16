using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceManagementAPI.Model
{
    [Table("puntos_emision")]
    public class PuntoEmision
    {
        [Key]
        [Column("pem_id")]
        public int PemId { get; set; }

        [Column("pem_id_punto_emision")]
        [MaxLength(3)]
        [Required]
        public string PemIdPuntoEmision { get; set; }

        [Column("est_ruc")]
        [MaxLength(13)]
        [Required]
        public string EstRuc { get; set; }

        [Column("pem_descripcion")]
        [MaxLength(100)]
        public string PemDescripcion { get; set; }

        [Column("pem_estado")]
        [Required]
        public bool PemEstado { get; set; } = true;

        // Relaciones
        [ForeignKey("EstRuc")]
        public virtual Establecimiento Establecimiento { get; set; }

        public virtual ICollection<Factura> Facturas { get; set; }
        public virtual ICollection<SecuencialFactura> SecuencialesFactura { get; set; }
    }

}