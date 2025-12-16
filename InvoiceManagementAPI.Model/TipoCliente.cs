using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Model
{
    // Tabla: tipos_cliente
    [Table("tipos_cliente")]
    public class TipoCliente
    {
        [Key]
        [Column("tic_id_tipoc")]
        public int TicIdTipoc { get; set; }

        [Column("tic_descripcion")]
        [MaxLength(20)]
        public string? TicDescripcion { get; set; }

        // Relaciones
        public virtual ICollection<Cliente> Clientes { get; set; }
    }
}
