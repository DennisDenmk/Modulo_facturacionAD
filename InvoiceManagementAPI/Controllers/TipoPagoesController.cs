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
    public class TipoPagoesController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public TipoPagoesController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/TipoPagoes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TipoPago>>> GetTiposPago()
        {
            return await _context.TiposPago.ToListAsync();
        }

        // GET: api/TipoPagoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TipoPago>> GetTipoPago(int id)
        {
            var tipoPago = await _context.TiposPago.FindAsync(id);

            if (tipoPago == null)
            {
                return NotFound();
            }

            return tipoPago;
        }

        // PUT: api/TipoPagoes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTipoPago(int id, TipoPago tipoPago)
        {
            if (id != tipoPago.TipIdTipop)
            {
                return BadRequest();
            }

            _context.Entry(tipoPago).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TipoPagoExists(id))
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

        // POST: api/TipoPagoes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TipoPago>> PostTipoPago(TipoPago tipoPago)
        {
            _context.TiposPago.Add(tipoPago);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTipoPago", new { id = tipoPago.TipIdTipop }, tipoPago);
        }

        // DELETE: api/TipoPagoes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTipoPago(int id)
        {
            var tipoPago = await _context.TiposPago.FindAsync(id);
            if (tipoPago == null)
            {
                return NotFound();
            }

            _context.TiposPago.Remove(tipoPago);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TipoPagoExists(int id)
        {
            return _context.TiposPago.Any(e => e.TipIdTipop == id);
        }
    }
}
