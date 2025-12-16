using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Model
{
    // Tabla: tipos_pago
    [Table("tipos_pago")]
    public class TipoPago
    {
        [Key]
        [Column("tip_id_tipop")]
        public int TipIdTipop { get; set; }

        [Column("tip_descripcion")]
        [MaxLength(20)]
        public string TipDescripcion { get; set; }

        // Relaciones
        public virtual ICollection<Factura> Facturas { get; set; }
    }
}
