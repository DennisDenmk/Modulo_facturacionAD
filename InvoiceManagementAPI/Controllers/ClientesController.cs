using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public ClientesController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/Clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            return await _context.Clientes.ToListAsync();
        }

        // GET: api/Clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(string id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            return cliente;
        }

        // PUT: api/Clientes/5

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(string id, Cliente cliente)
        {
            if (id != cliente.CliCedula)
            {
                return BadRequest(new { message = "La cédula de la URL no coincide con la del cuerpo de la solicitud." });
            }

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException as MySqlException;
                if (sqlException != null)
                {
                    if (sqlException.Number == 1062 || sqlException.Message.Contains("Cliente ya registrado"))
                    {
                        return Conflict(new { message = "Error: Cliente ya registrado" });
                    }
                    return BadRequest(new { message = sqlException.Message });
                }
                throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                
                var sqlException = ex.InnerException as MySqlException;

                if (sqlException != null)
                {
                    
                    if (sqlException.Number == 1062 || sqlException.Message.Contains("Cliente ya registrado"))
                    {
                       
                        return Conflict(new { message = "Error: Cliente ya registrado" });
                    }

                   return BadRequest(new { message = sqlException.Message });
                }

                throw;
            }

            return CreatedAtAction("GetCliente", new { id = cliente.CliCedula }, cliente);
        }

        // ==================================================
        // PATCH: api/Clientes/{id}/estado
        // Cambiar estado de un cliente (Activar/Desactivar)
        // ==================================================

        [HttpPatch("{id}/estado")]
        public async Task<ActionResult> CambiarEstadoCliente(
            string id,
            [FromBody] CambioEstadoRequest request)
        {
            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.CliCedula == id);

                if (cliente == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Cliente con cédula {id} no encontrado"
                    });
                }

                // Cambiar el estado
                cliente.CliEstado = request.Estado;
                cliente.CliFechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Cliente {(request.Estado ? "activado" : "desactivado")} exitosamente",
                    data = new
                    {
                        cliente.CliCedula,
                        cliente.CliNombre,
                        cliente.CliEstado,
                        cliente.CliFechaModificacion
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al cambiar el estado del cliente",
                    error = ex.Message
                });
            }
        }
        

        // ==================================================
        // GET: api/Clientes/activos
        // Obtener clientes activos
        // ==================================================
        [HttpGet("activos")]
        public async Task<ActionResult> GetClientesActivos()
        {
            try
            {
                var clientes = await _context.Clientes
                    .Include(c => c.TipoCliente)
                    .Where(c => c.CliEstado == true)
                    .Select(c => new
                    {
                        c.CliCedula,
                        c.CliNombre,
                        c.CliFechaNacimiento, // Agregado: campo faltante
                        c.TicIdTipoc,         // Agregado: para logica de respaldo
                        c.CliDireccion,
                        c.CliTelefono,
                        c.CliEmail,
                        TipoCliente = new {   // Devolver objeto para coincidir con la expectativa del frontend
                             TicDescripcion = c.TipoCliente != null ? c.TipoCliente.TicDescripcion : null
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true
                    
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener clientes activos",
                    error = ex.Message
                });
            }
        }

        //Metodos exta
        private bool ClienteExists(string id)
        {
            return _context.Clientes.Any(e => e.CliCedula == id);
        }

        public class CambioEstadoRequest
        {
            public bool Estado { get; set; }
        }

        // ==================================================
        // GET: api/Clientes/clientes-a-credito
        // Obtener clientes que son de tipo crédito
        // ==================================================
        [HttpGet("clientes-tipo-credito")]
        public async Task<ActionResult> GetClientesACredito()
        {
            try
            {
                var clientesCredito = await _context.Clientes
                    .Include(c => c.TipoCliente)
                    .Where(c => c.CliEstado == true &&
                               c.TipoCliente != null &&
                               c.TipoCliente.TicDescripcion.ToLower().Contains("crédito"))
                    .Select(c => new
                    {
                        c.CliCedula,
                        c.CliNombre,
                        c.CliFechaNacimiento,
                        c.TicIdTipoc,
                        c.CliDireccion,
                        c.CliTelefono,
                        c.CliEmail,
                        TipoCliente = new
                        {
                            c.TipoCliente.TicIdTipoc,
                            c.TipoCliente.TicDescripcion
                        }
                    })
                    .ToListAsync();

                return Ok(clientesCredito);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener clientes a crédito",
                    error = ex.Message
                });
            }
        }

    }
}
