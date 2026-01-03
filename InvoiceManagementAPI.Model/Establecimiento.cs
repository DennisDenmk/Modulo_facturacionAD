using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Model
{
    // Tabla: establecimientos
    [Table("establecimientos")]
    public class Establecimiento
    {
        [Key]
        [Column("est_ruc")]
        [MaxLength(13)]
        public string EstRuc { get; set; }

        [Column("est_nombre")]
        [MaxLength(100)]
        public string EstNombre { get; set; }

        [Column("est_direccion")]
        [MaxLength(255)]
        public string EstDireccion { get; set; }

        [Column("est_telefono")]
        [MaxLength(15)]
        public string EstTelefono { get; set; }

        [Column("est_estado")]
        public bool EstEstado { get; set; }

        // Relaciones
        public virtual ICollection<PuntoEmision>? PuntosEmision { get; set; }
    }
}
