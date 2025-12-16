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
    public class SecuencialFacturasController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public SecuencialFacturasController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/SecuencialFacturas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SecuencialFactura>>> GetSecuencialesFactura()
        {
            return await _context.SecuencialesFactura.ToListAsync();
        }

        // GET: api/SecuencialFacturas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SecuencialFactura>> GetSecuencialFactura(int id)
        {
            var secuencialFactura = await _context.SecuencialesFactura.FindAsync(id);

            if (secuencialFactura == null)
            {
                return NotFound();
            }

            return secuencialFactura;
        }

        // PUT: api/SecuencialFacturas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSecuencialFactura(int id, SecuencialFactura secuencialFactura)
        {
            if (id != secuencialFactura.SecIdSecuencial)
            {
                return BadRequest();
            }

            _context.Entry(secuencialFactura).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SecuencialFacturaExists(id))
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

        // POST: api/SecuencialFacturas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SecuencialFactura>> PostSecuencialFactura(SecuencialFactura secuencialFactura)
        {
            _context.SecuencialesFactura.Add(secuencialFactura);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSecuencialFactura", new { id = secuencialFactura.SecIdSecuencial }, secuencialFactura);
        }

        // DELETE: api/SecuencialFacturas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSecuencialFactura(int id)
        {
            var secuencialFactura = await _context.SecuencialesFactura.FindAsync(id);
            if (secuencialFactura == null)
            {
                return NotFound();
            }

            _context.SecuencialesFactura.Remove(secuencialFactura);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SecuencialFacturaExists(int id)
        {
            return _context.SecuencialesFactura.Any(e => e.SecIdSecuencial == id);
        }
    }
}
