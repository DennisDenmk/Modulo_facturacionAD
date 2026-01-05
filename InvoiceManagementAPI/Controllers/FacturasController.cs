using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Model;
using InvoiceManagementAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace InvoiceManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturasController : ControllerBase
    {
        private readonly InvoiceDbContext _context;
        private readonly IInventoryService _inventoryService;

        public FacturasController(InvoiceDbContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // =========================================================================
        //  MÉTODOS GET (Lectura y Reportes)
        // =========================================================================

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
            var factura = await _context.Facturas
                .Include(f => f.DetallesFactura) // Incluimos detalles al buscar una específica
                .FirstOrDefaultAsync(f => f.FacNumeroFactura == id);

            if (factura == null)
            {
                return NotFound();
            }

            return factura;
        }

        // GET: api/Facturas/credito-por-cliente/{cedula}
        [HttpGet("credito-por-cliente/{cedula}")]
        public async Task<ActionResult> GetFacturasCreditoPorCliente(string cedula)
        {
            try
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.CliCedula == cedula);

                if (cliente == null)
                {
                    return NotFound(new { success = false, message = $"Cliente con cédula {cedula} no encontrado" });
                }

                var facturasCredito = await _context.Facturas
                    .Include(f => f.TipoPago)
                    .Include(f => f.DetallesFactura)
                    .Where(f => f.CliCedula == cedula && f.TipoPago.TipDescripcion.ToLower().Contains("credito"))
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        f.FacNumeroFactura,
                        f.FacFechaEmision,
                        TipoPago = f.TipoPago.TipDescripcion,
                        f.FacTotal,
                        f.FacEstadoFactura,
                        Estado = f.FacEstadoFactura ? "Activa" : "Anulada",
                        CantidadProductos = f.DetallesFactura.Count
                    })
                    .ToListAsync();

                var totalCredito = facturasCredito.Where(f => f.FacEstadoFactura).Sum(f => f.FacTotal);

                return Ok(new
                {
                    success = true,
                    cliente = new { cliente.CliCedula, cliente.CliNombre, cliente.CliEmail },
                    resumen = new { totalFacturas = facturasCredito.Count, totalCredito, facturasPendientes = facturasCredito.Count(f => f.FacEstadoFactura) },
                    facturas = facturasCredito
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error interno", error = ex.Message });
            }
        }

        // GET: api/Facturas/detallado-por-productos
        [HttpGet("detallado-por-productos")]
        public async Task<ActionResult> GetFacturasDetalladoPorProductos([FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var query = _context.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.DetallesFactura)
                    .AsQueryable();

                if (fechaInicio.HasValue) query = query.Where(f => f.FacFechaEmision >= fechaInicio.Value);
                if (fechaFin.HasValue) query = query.Where(f => f.FacFechaEmision <= fechaFin.Value);

                var facturasDetalladas = await query
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        Factura = new { f.FacNumeroFactura, f.FacFechaEmision, f.FacTotal },
                        Cliente = new { f.Cliente.CliNombre },
                        Productos = f.DetallesFactura.Select(d => new
                        {
                            d.DetCodigoProducto,
                            d.DetNombreProducto,
                            d.DetCantidad,
                            d.DetTotal
                        }).ToList()
                    }).ToListAsync();

                return Ok(new { success = true, data = facturasDetalladas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener reporte", error = ex.Message });
            }
        }

        // =========================================================================
        //  MÉTODO POST (NUEVA LÓGICA CON INVENTARIO)
        // =========================================================================

        [HttpPost]
        public async Task<ActionResult<Factura>> PostFactura(Factura factura)
        {
            // 1. Validaciones Iniciales
            if (factura.DetallesFactura == null || !factura.DetallesFactura.Any())
            {
                return BadRequest("La factura debe tener al menos un detalle.");
            }

            // ---------------------------------------------------------------------------
            // SOLUCIÓN AL ERROR "MySqlRetryingExecutionStrategy":
            // Creamos una estrategia de ejecución para envolver la transacción
            // ---------------------------------------------------------------------------
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Ahora sí podemos abrir la transacción DENTRO de la estrategia
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // -----------------------------------------------------------------------
                    // PASO 1: PRE-CÁLCULO Y VALIDACIÓN CON API EXTERNA
                    // -----------------------------------------------------------------------
                    decimal acumuladorSubtotalSinIva = 0;
                    decimal acumuladorSubtotalConIva = 0;
                    decimal acumuladorIva = 0;

                    var detallesProcesados = new List<DetalleFactura>();

                    foreach (var detalle in factura.DetallesFactura)
                    {
                        if (string.IsNullOrEmpty(detalle.DetCodigoProducto))
                            return BadRequest("El código del producto es obligatorio.");
                        if (!detalle.DetCantidad.HasValue || detalle.DetCantidad <= 0)
                            return BadRequest($"Cantidad inválida para {detalle.DetCodigoProducto}.");

                        var productoExterno = await _inventoryService.ObtenerProductoPorCodigo(detalle.DetCodigoProducto);

                        if (productoExterno == null)
                            return BadRequest($"El producto '{detalle.DetCodigoProducto}' no existe en el inventario externo.");

                        if (productoExterno.Stock < detalle.DetCantidad.Value)
                            return BadRequest($"Stock insuficiente para '{productoExterno.Nombre}'.");

                        // Mapeo
                        detalle.DetIdProductoExterno = productoExterno.Id;
                        detalle.DetNombreProducto = productoExterno.Nombre;
                        detalle.DetPrecioUnitario = productoExterno.PrecioVenta;

                        // Cálculos
                        detalle.DetSubtotal = detalle.DetCantidad.Value * detalle.DetPrecioUnitario.Value;
                        decimal porcentajeIva = productoExterno.Impuesto.Porcentaje;
                        detalle.DetPorcentajeIva = porcentajeIva;
                        detalle.DetGrabaIva = porcentajeIva > 0;

                        if (detalle.DetGrabaIva.Value)
                        {
                            detalle.DetValorIva = detalle.DetSubtotal.Value * (porcentajeIva / 100m);
                            acumuladorSubtotalConIva += detalle.DetSubtotal.Value;
                        }
                        else
                        {
                            detalle.DetValorIva = 0;
                            acumuladorSubtotalSinIva += detalle.DetSubtotal.Value;
                        }

                        detalle.DetTotal = detalle.DetSubtotal.Value + detalle.DetValorIva.Value;
                        acumuladorIva += detalle.DetValorIva.Value;

                        detallesProcesados.Add(detalle);
                    }

                    decimal totalFinal = acumuladorSubtotalSinIva + acumuladorSubtotalConIva + acumuladorIva;

                    // -----------------------------------------------------------------------
                    // PASO 2: LLAMAR AL SP (ADO.NET DENTRO DE ESTRATEGIA)
                    // -----------------------------------------------------------------------

                    var connection = _context.Database.GetDbConnection();
                    using var command = connection.CreateCommand();

                    // Vinculamos a la transacción actual
                    if (command.Transaction == null && _context.Database.CurrentTransaction != null)
                    {
                        command.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();
                    }

                    command.CommandText = "sp_crear_factura";
                    command.CommandType = CommandType.StoredProcedure;

                    // Helper local
                    void AddParam(string name, object value)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = name;
                        p.Value = value ?? DBNull.Value;
                        command.Parameters.Add(p);
                    }

                    AddParam("@p_cli_cedula", factura.CliCedula);
                    AddParam("@p_est_ruc", factura.EstRuc);
                    AddParam("@p_pem_id", factura.PemId);
                    AddParam("@p_tip_id_tipop", factura.TipIdTipop);
                    AddParam("@p_fac_subtotal_sin_iva", acumuladorSubtotalSinIva);
                    AddParam("@p_fac_subtotal_con_iva", acumuladorSubtotalConIva);
                    AddParam("@p_fac_iva", acumuladorIva);
                    AddParam("@p_fac_total", totalFinal);
                    AddParam("@p_fac_estado_factura", factura.FacEstadoFactura);
                    AddParam("@p_fac_usuario_creacion", factura.FacUsuarioCreacion);

                    // Outputs
                    var pOutNumero = command.CreateParameter();
                    pOutNumero.ParameterName = "@p_numero_factura";
                    pOutNumero.Direction = ParameterDirection.Output;
                    pOutNumero.DbType = DbType.String;
                    pOutNumero.Size = 15;
                    command.Parameters.Add(pOutNumero);

                    var pOutFecha = command.CreateParameter();
                    pOutFecha.ParameterName = "@p_fecha_emision";
                    pOutFecha.Direction = ParameterDirection.Output;
                    pOutFecha.DbType = DbType.DateTime;
                    command.Parameters.Add(pOutFecha);

                    // Ejecutar
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    string nuevoNumeroFactura = pOutNumero.Value?.ToString();

                    if (string.IsNullOrEmpty(nuevoNumeroFactura))
                        throw new Exception("El procedimiento almacenado no devolvió un número de factura.");

                    // Asignar al objeto
                    factura.FacNumeroFactura = nuevoNumeroFactura;
                    if (DateTime.TryParse(pOutFecha.Value?.ToString(), out DateTime fechaEmision))
                    {
                        factura.FacFechaEmision = fechaEmision;
                    }

                    // -----------------------------------------------------------------------
                    // PASO 3: GUARDAR DETALLES
                    // -----------------------------------------------------------------------
                    foreach (var detalle in detallesProcesados)
                    {
                        detalle.FacNumeroFactura = nuevoNumeroFactura;
                    }

                    _context.DetallesFactura.AddRange(detallesProcesados);
                    await _context.SaveChangesAsync();

                    // -----------------------------------------------------------------------
                    // PASO 4: COMMIT
                    // -----------------------------------------------------------------------
                    await transaction.CommitAsync();

                    // Asignar totales calculados al response
                    factura.FacSubtotalSinIva = acumuladorSubtotalSinIva;
                    factura.FacSubtotalConIva = acumuladorSubtotalConIva;
                    factura.FacIva = acumuladorIva;
                    factura.FacTotal = totalFinal;

                    return CreatedAtAction("GetFactura", new { id = nuevoNumeroFactura }, factura);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var mensajeError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    // IMPORTANTE: Devolvemos un ObjectResult para que coincida con la firma del método
                    return StatusCode(500, new { success = false, message = "Error procesando factura", error = mensajeError });
                }
            });
        }

        // =========================================================================
        //  MÉTODOS PUT y DELETE
        // =========================================================================

        // PUT: api/Facturas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFactura(string id, Factura factura)
        {
            if (id != factura.FacNumeroFactura) return BadRequest();

            _context.Entry(factura).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacturaExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Facturas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFactura(string id)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura == null) return NotFound();

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