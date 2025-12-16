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
    public class PuntoEmisionsController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public PuntoEmisionsController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/PuntoEmisions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PuntoEmision>>> GetPuntosEmision()
        {
            return await _context.PuntosEmision
                .Include(p => p.Establecimiento)
                .ToListAsync();
        }

        // GET: api/PuntoEmisions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PuntoEmision>> GetPuntoEmision(int id)
        {
            var puntoEmision = await _context.PuntosEmision
                .Include(p => p.Establecimiento)
                .FirstOrDefaultAsync(p => p.PemId == id);

            if (puntoEmision == null)
            {
                return NotFound();
            }

            return puntoEmision;
        }

        // PUT: api/PuntoEmisions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPuntoEmision(int id, PuntoEmision puntoEmision)
        {
            if (id != puntoEmision.PemId)
            {
                return BadRequest();
            }

            _context.Entry(puntoEmision).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PuntoEmisionExists(id))
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

        // POST: api/PuntoEmisions
        [HttpPost]
        public async Task<ActionResult<PuntoEmision>> PostPuntoEmision(PuntoEmision puntoEmision)
        {
            _context.PuntosEmision.Add(puntoEmision);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (PuntoEmisionExists(puntoEmision.PemId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetPuntoEmision", new { id = puntoEmision.PemId }, puntoEmision);
        }

        // PATCH: api/PuntoEmisions/5/estado
        // Cambiar estado de un punto de emisión
        [HttpPatch("{id}/estado")]
        public async Task<ActionResult> PatchPuntoEmisionEstado(
            int id,
            [FromBody] PuntoEmisionEstadoUpdate request)
        {
            try
            {
                var puntoEmision = await _context.PuntosEmision.FindAsync(id);

                if (puntoEmision == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Punto de emisión con ID {id} no encontrado"
                    });
                }

                // Cambiar el estado
                puntoEmision.PemEstado = request.PemEstado;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Punto de emisión {(request.PemEstado ? "activado" : "desactivado")} exitosamente",
                    data = new
                    {
                        puntoEmision.PemId,
                        puntoEmision.PemIdPuntoEmision,
                        puntoEmision.PemDescripcion,
                        puntoEmision.PemEstado,
                        puntoEmision.EstRuc
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al cambiar el estado del punto de emisión",
                    error = ex.Message
                });
            }
        }

        private bool PuntoEmisionExists(int id)
        {
            return _context.PuntosEmision.Any(e => e.PemId == id);
        }

        public class PuntoEmisionEstadoUpdate
        {
            public bool PemEstado { get; set; }
        }
    }
}