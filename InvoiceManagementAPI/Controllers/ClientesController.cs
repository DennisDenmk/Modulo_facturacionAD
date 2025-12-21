using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Model;

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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(string id, Cliente cliente)
        {
            if (id != cliente.CliCedula)
            {
                return BadRequest();
            }

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Clientes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ClienteExists(cliente.CliCedula))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
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
                    success = true,
                    total = clientes.Count,
                    data = clientes
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

    }
}
