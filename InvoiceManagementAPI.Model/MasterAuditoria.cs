using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Model
{
    [Table("pistas_auditoria")]
    public class MasterAuditoria
    {
        [Key]
        [Column("aud_id_auditoria")]
        public int AudIdAuditoria { get; set; }

        [Required]
        [Column("aud_usuario")]
        [MaxLength(50)]
        public string AudUsuario { get; set; }

        [Required]
        [Column("aud_accion")]
        [MaxLength(50)]
        public string AudAccion { get; set; }

        [Column("aud_tabla_afectada")]
        [MaxLength(50)]
        public string? AudTablaAfectada { get; set; }

        [Column("aud_id_registro")]
        [MaxLength(50)]
        public string? AudIdRegistro { get; set; }

        [Column("aud_datos_anteriores", TypeName = "json")]
        public string? AudDatosAnteriores { get; set; }

        [Column("aud_datos_nuevos", TypeName = "json")]
        public string? AudDatosNuevos { get; set; }

        [Column("aud_direccion_ip")]
        [MaxLength(45)]
        public string? AudDireccionIp { get; set; }

        [Column("aud_navegador")]
        [MaxLength(200)]
        public string? AudNavegador { get; set; }

        [Required]
        [Column("aud_fecha_hora")]
        public DateTime AudFechaHora { get; set; }
    }
}

