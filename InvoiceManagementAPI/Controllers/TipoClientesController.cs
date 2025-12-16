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
    public class TipoClientesController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public TipoClientesController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/TipoClientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TipoCliente>>> GetTiposCliente()
        {
            return await _context.TiposCliente.ToListAsync();
        }

        // GET: api/TipoClientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TipoCliente>> GetTipoCliente(int id)
        {
            var tipoCliente = await _context.TiposCliente.FindAsync(id);

            if (tipoCliente == null)
            {
                return NotFound();
            }

            return tipoCliente;
        }

        // PUT: api/TipoClientes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTipoCliente(int id, TipoCliente tipoCliente)
        {
            if (id != tipoCliente.TicIdTipoc)
            {
                return BadRequest();
            }

            _context.Entry(tipoCliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TipoClienteExists(id))
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

        // POST: api/TipoClientes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TipoCliente>> PostTipoCliente(TipoCliente tipoCliente)
        {
            _context.TiposCliente.Add(tipoCliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTipoCliente", new { id = tipoCliente.TicIdTipoc }, tipoCliente);
        }

        // DELETE: api/TipoClientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTipoCliente(int id)
        {
            var tipoCliente = await _context.TiposCliente.FindAsync(id);
            if (tipoCliente == null)
            {
                return NotFound();
            }

            _context.TiposCliente.Remove(tipoCliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TipoClienteExists(int id)
        {
            return _context.TiposCliente.Any(e => e.TicIdTipoc == id);
        }
    }
}
