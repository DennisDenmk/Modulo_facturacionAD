using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Model;
using MySqlConnector; // ✅ Asegúrate de que esto esté presente

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
        // ⚠️ ASEGÚRATE DE QUE SOLO HAYA UN MÉTODO PUT
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPuntoEmision(int id, PuntoEmision puntoEmision)
        {
            if (id != puntoEmision.PemId)
            {
                return BadRequest(new { message = "El ID no coincide" });
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
            catch (DbUpdateException ex)
            {
                // Buscar la excepción de MySQL en la cadena de excepciones
                Exception innerException = ex.InnerException;

                while (innerException != null)
                {
                    if (innerException is MySqlException mySqlEx)
                    {
                        // Error 45000: SIGNAL del trigger
                        // Error 1062: Duplicate entry (UNIQUE constraint)
                        if (mySqlEx.Number == 45000 || mySqlEx.Number == 1062)
                        {
                            return Conflict(new { message = mySqlEx.Message });
                        }
                    }
                    innerException = innerException.InnerException;
                }

                // Si llegamos aquí, no fue un error de duplicado
                throw;
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
                return CreatedAtAction("GetPuntoEmision", new { id = puntoEmision.PemId }, puntoEmision);
            }
            catch (Exception ex)
            {
                // Buscar MySqlException en la cadena
                Exception current = ex;
                while (current != null)
                {
                    if (current is MySqlConnector.MySqlException mySqlEx)
                    {
                        if (mySqlEx.Number == 45000 || mySqlEx.Number == 1062)
                        {
                            return Conflict(new { message = mySqlEx.Message });
                        }
                    }
                    current = current.InnerException;
                }

                // Si no es error de duplicado, re-lanzar
                throw;
            }
        }

        // PATCH: api/PuntoEmisions/5/estado
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