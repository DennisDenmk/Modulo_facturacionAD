using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        [ForeignKey("EstRuc")]
        public virtual Establecimiento? Establecimiento { get; set; }

        // En las listas sí dejamos JsonIgnore para no sobrecargar datos
        [JsonIgnore]
        public virtual ICollection<Factura>? Facturas { get; set; }

        [JsonIgnore]
        public virtual ICollection<SecuencialFactura>? SecuencialesFactura { get; set; }
    }
}