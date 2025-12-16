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
    public class MasterAuditoriasController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public MasterAuditoriasController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/MasterAuditorias
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MasterAuditoria>>> GetAuditorias()
        {
            return await _context.Auditorias.ToListAsync();
        }

        // GET: api/MasterAuditorias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MasterAuditoria>> GetMasterAuditoria(int id)
        {
            var masterAuditoria = await _context.Auditorias.FindAsync(id);

            if (masterAuditoria == null)
            {
                return NotFound();
            }

            return masterAuditoria;
        }

       

        private bool MasterAuditoriaExists(int id)
        {
            return _context.Auditorias.Any(e => e.AudIdAuditoria == id);
        }
    }
}
