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
    public class FacturasController : ControllerBase
    {
        private readonly InvoiceDbContext _context;

        public FacturasController(InvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/Facturas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Factura>>> GetFacturas()
        {
            return await _context.Facturas.ToListAsync();
        }

        // GET: api/Facturas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Factura>> GetFactura(string id)
        {
            var factura = await _context.Facturas.FindAsync(id);

            if (factura == null)
            {
                return NotFound();
            }

            return factura;
        }

        // FACTURAS A CRÉDITO POR CLIENTE
        // GET: api/Facturas/credito-por-cliente/{cedula}

        [HttpGet("credito-por-cliente/{cedula}")]
        public async Task<ActionResult> GetFacturasCreditoPorCliente(string cedula)
        {
            try
            {
                // Buscar el cliente
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.CliCedula == cedula);

                if (cliente == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Cliente con cédula {cedula} no encontrado"
                    });
                }

                // Obtener facturas a crédito del cliente
                var facturasCredito = await _context.Facturas
                    .Include(f => f.TipoPago)
                    .Include(f => f.DetallesFactura)
                    .Where(f => f.CliCedula == cedula &&
                               f.TipoPago.TipDescripcion.ToLower().Contains("credito"))
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        f.FacNumeroFactura,
                        f.FacFechaEmision,
                        TipoPago = f.TipoPago.TipDescripcion,
                        f.FacSubtotalSinIva,
                        f.FacSubtotalConIva,
                        f.FacIva,
                        f.FacTotal,
                        f.FacEstadoFactura,
                        Estado = f.FacEstadoFactura == true ? "Activa" : "Anulada",
                        CantidadProductos = f.DetallesFactura.Count
                    })
                    .ToListAsync();

                var totalCredito = facturasCredito
                    .Where(f => f.FacEstadoFactura == true)
                    .Sum(f => f.FacTotal);

                return Ok(new
                {
                    success = true,
                    cliente = new
                    {
                        cliente.CliCedula,
                        cliente.CliNombre,
                        cliente.CliEmail,
                        cliente.CliTelefono
                    },
                    resumen = new
                    {
                        totalFacturas = facturasCredito.Count,
                        totalCredito = totalCredito,
                        facturasPendientes = facturasCredito.Count(f => f.FacEstadoFactura == true)
                    },
                    facturas = facturasCredito
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener facturas a crédito",
                    error = ex.Message
                });
            }
        }

        // FACTURAS DETALLADAS POR PRODUCTOS
        // GET: api/Facturas/detallado-por-productos
        // Parámetros opcionales: ?fechaInicio=2024-01-01&fechaFin=2024-12-31
        [HttpGet("detallado-por-productos")]
        public async Task<ActionResult> GetFacturasDetalladoPorProductos(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var query = _context.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.TipoPago)
                    .Include(f => f.DetallesFactura)
                    .AsQueryable();

                // Aplicar filtros de fecha si existen
                if (fechaInicio.HasValue)
                {
                    query = query.Where(f => f.FacFechaEmision >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(f => f.FacFechaEmision <= fechaFin.Value);
                }

                var facturasDetalladas = await query
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        // Información de la factura
                        Factura = new
                        {
                            f.FacNumeroFactura,
                            f.FacFechaEmision,
                            f.FacTotal,
                            f.FacIva,
                            f.FacEstadoFactura,
                            TipoPago = f.TipoPago.TipDescripcion
                        },
                        // Información del cliente
                        Cliente = new
                        {
                            f.Cliente.CliCedula,
                            f.Cliente.CliNombre,
                            f.Cliente.CliEmail
                        },
                        // Detalle de productos
                        Productos = f.DetallesFactura.Select(d => new
                        {
                            d.DetIdDetalle,
                            d.DetCodigoProducto,
                            d.DetNombreProducto,
                            d.DetCantidad,
                            d.DetPrecioUnitario,
                            d.DetGrabaIva,
                            d.DetSubtotal,
                            d.DetValorIva,
                            d.DetTotal,
                            PorcentajeIva = d.DetPorcentajeIva
                        }).ToList()
                    })
                    .ToListAsync();

                // Resumen de productos vendidos
                var resumenProductos = await _context.DetallesFactura
                    .Include(d => d.Factura)
                    .Where(d => (!fechaInicio.HasValue || d.Factura.FacFechaEmision >= fechaInicio.Value) &&
                               (!fechaFin.HasValue || d.Factura.FacFechaEmision <= fechaFin.Value))
                    .GroupBy(d => new { d.DetCodigoProducto, d.DetNombreProducto })
                    .Select(g => new
                    {
                        CodigoProducto = g.Key.DetCodigoProducto,
                        NombreProducto = g.Key.DetNombreProducto,
                        CantidadVendida = g.Sum(d => d.DetCantidad),
                        TotalVentas = g.Sum(d => d.DetTotal),
                        NumeroFacturas = g.Count()
                    })
                    .OrderByDescending(p => p.TotalVentas)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    filtros = new
                    {
                        fechaInicio = fechaInicio?.ToString("yyyy-MM-dd"),
                        fechaFin = fechaFin?.ToString("yyyy-MM-dd")
                    },
                    resumen = new
                    {
                        totalFacturas = facturasDetalladas.Count,
                        totalVentas = facturasDetalladas.Sum(f => f.Factura.FacTotal),
                        productosUnicos = resumenProductos.Count
                    },
                    facturasDetalladas = facturasDetalladas,
                    resumenPorProducto = resumenProductos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener facturas detalladas",
                    error = ex.Message
                });
            }
        }


        // PUT: api/Facturas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFactura(string id, Factura factura)
        {
            if (id != factura.FacNumeroFactura)
            {
                return BadRequest();
            }

            _context.Entry(factura).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacturaExists(id))
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

        // POST: api/Facturas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Factura>> PostFactura(Factura factura)
        {
            _context.Facturas.Add(factura);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (FacturaExists(factura.FacNumeroFactura))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetFactura", new { id = factura.FacNumeroFactura }, factura);
        }

        // DELETE: api/Facturas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFactura(string id)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura == null)
            {
                return NotFound();
            }

            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FacturaExists(string id)
        {
            return _context.Facturas.Any(e => e.FacNumeroFactura == id);
        }
    }
}
