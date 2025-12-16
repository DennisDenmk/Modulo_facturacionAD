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
    public class EstablecimientoesController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public EstablecimientoesController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/Establecimientoes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Establecimiento>>> GetEstablecimientos()
        {
            return await _context.Establecimientos.ToListAsync();
        }

        // GET: api/Establecimientoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Establecimiento>> GetEstablecimiento(string id)
        {
            var establecimiento = await _context.Establecimientos.FindAsync(id);

            if (establecimiento == null)
            {
                return NotFound();
            }

            return establecimiento;
        }

        // PUT: api/Establecimientoes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEstablecimiento(string id, Establecimiento establecimiento)
        {
            if (id != establecimiento.EstRuc)
            {
                return BadRequest();
            }

            _context.Entry(establecimiento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EstablecimientoExists(id))
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

        // POST: api/Establecimientoes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Establecimiento>> PostEstablecimiento(Establecimiento establecimiento)
        {
            _context.Establecimientos.Add(establecimiento);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (EstablecimientoExists(establecimiento.EstRuc))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetEstablecimiento", new { id = establecimiento.EstRuc }, establecimiento);
        }

        
        // PATCH: api/Establecimientoes/{id}/estado
        
        [HttpPatch("{id}/estado")]
        public async Task<ActionResult> PatchEstablecimientoEstado(
            string id,
            [FromBody] EstablecimientoEstadoUpdate request)
        {
            try
            {
                var establecimiento = await _context.Establecimientos.FindAsync(id);

                if (establecimiento == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Establecimiento con RUC {id} no encontrado"
                    });
                }

                // Cambiar el estado
                establecimiento.EstEstado = request.EstEstado;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Establecimiento {(request.EstEstado ? "activado" : "desactivado")} exitosamente",
                    data = new
                    {
                        establecimiento.EstRuc,
                        establecimiento.EstNombre,
                        establecimiento.EstEstado
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al cambiar el estado del establecimiento",
                    error = ex.Message
                });
            }
        }

        //Establecimientos activos
        [HttpGet("activos")]
        public async Task<ActionResult<IEnumerable<Establecimiento>>> GetEstablecimientosActivos()
        {
            return await _context.Establecimientos
                .Where(e => e.EstEstado == true)
                .ToListAsync();
        }


        private bool EstablecimientoExists(string id)
        {
            return _context.Establecimientos.Any(e => e.EstRuc == id);
        }

        public class EstablecimientoEstadoUpdate
        {
            public bool EstEstado { get; set; }
        }

    }


}
