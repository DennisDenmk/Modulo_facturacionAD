using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Model
{
    // Tabla: clientes
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Column("cli_cedula")]
        [MaxLength(10)]
        public required string CliCedula { get; set; }

        [Column("cli_nombre")]
        [MaxLength(200)]
        public required string CliNombre { get; set; }

        [Column("cli_fecha_nacimiento")]
        public DateTime? CliFechaNacimiento { get; set; }

        [Column("tic_id_tipoc")]
        public int? TicIdTipoc { get; set; }


        [Column("cli_direccion")]
        [MaxLength(255)]
        public string? CliDireccion { get; set; }

        [Column("cli_telefono")]
        [MaxLength(15)]
        public string? CliTelefono { get; set; }

        [Column("cli_email")]
        [MaxLength(100)]
        public string? CliEmail { get; set; }

        [Column("cli_estado")]
        public bool? CliEstado { get; set; }

        [Column("cli_fecha_creacion")]
        public DateTime? CliFechaCreacion { get; set; } = DateTime.Now;

        [Column("cli_fecha_modificacion")]
        public DateTime? CliFechaModificacion { get; set; }

        [Column("cli_usuario_creacion")]
        [MaxLength(50)]
        public string? CliUsuarioCreacion { get; set; }

        [Column("cli_usuario_modificacion")]
        [MaxLength(50)]
        public string? CliUsuarioModificacion { get; set; }

        // Relaciones
        [ForeignKey("TicIdTipoc")]
        public virtual TipoCliente? TipoCliente { get; set; }

        public virtual ICollection<Factura>? Facturas { get; set; }
    }
}
