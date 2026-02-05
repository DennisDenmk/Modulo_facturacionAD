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

        // =========================================================================
        //  MÉTODO GET: Listado de Facturas a Crédito por Cliente
        //  Endpoint: api/Facturas/facturas-credito-por-cliente/{cedula}
        // =========================================================================

        /// <summary>
        /// Obtiene todas las facturas a crédito de un cliente específico
        /// Incluye detalles completos de productos
        /// Valida que el cliente exista y esté activo
        /// </summary>
        /// <param name="cedula">Cédula del cliente</param>
        /// <returns>Facturas a crédito del cliente con detalles completos</returns>
        [HttpGet("credito-por-cliente/{cedula}")]
        public async Task<ActionResult> GetFacturasCreditoPorCliente(string cedula)
        {
            try
            {
                // 1. Validar que el cliente exista
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

                // 3. Obtener facturas a crédito del cliente
                var facturasCredito = await _context.Facturas
                    .Include(f => f.TipoPago)
                    .Include(f => f.DetallesFactura)
                    .Where(f => f.CliCedula == cedula &&
                           f.TipoPago.TipDescripcion.ToLower().Contains("credito"))
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        numeroFactura = f.FacNumeroFactura,
                        fechaEmision = f.FacFechaEmision,
                        tipoPago = f.TipoPago.TipDescripcion,
                        subtotalSinIva = f.FacSubtotalSinIva,
                        subtotalConIva = f.FacSubtotalConIva,
                        iva = f.FacIva,
                        total = f.FacTotal,
                        estado = f.FacEstadoFactura,
                        productos = f.DetallesFactura.Select(d => new
                        {
                            codigo = d.DetCodigoProducto,
                            nombre = d.DetNombreProducto,
                            cantidad = d.DetCantidad,
                            precioUnitario = d.DetPrecioUnitario,
                            subtotal = d.DetSubtotal,
                            grabaIva = d.DetGrabaIva,
                            valorIva = d.DetValorIva,
                            total = d.DetTotal
                        }).ToList()
                    })
                    .ToListAsync();

                // 4. Retornar respuesta
                return Ok(new
                {
                    success = true,
                    cliente = new
                    {
                        cedula = cliente.CliCedula,
                        nombre = cliente.CliNombre,
                        email = cliente.CliEmail,
                        telefono = cliente.CliTelefono
                    },
                    facturas = facturasCredito
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener facturas a crédito del cliente",
                    error = ex.Message
                });
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


        private bool FacturaExists(string id)
        {
            return _context.Facturas.Any(e => e.FacNumeroFactura == id);
        }

        /// <summary>
        /// Obtiene un reporte detallado de todas las facturas con información completa de productos
        /// </summary>
        /// <param name="cliCedula">Cédula del cliente (opcional)</param>
        /// <returns>Lista de facturas con detalles completos de productos</returns>
        [HttpGet("detalladas-por-productos")]
        public async Task<ActionResult> GetFacturasDetalladasPorProductos(
            [FromQuery] string? cliCedula)
        {
            try
            {
                // Construir query base
                var query = _context.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.TipoPago)
                    .Include(f => f.PuntoEmision)
                        .ThenInclude(p => p.Establecimiento)
                    .Include(f => f.DetallesFactura)
                    .AsQueryable();

                // Aplicar filtro opcional por cliente
                if (!string.IsNullOrEmpty(cliCedula))
                {
                    query = query.Where(f => f.CliCedula == cliCedula);
                }

                // Ejecutar consulta con proyección
                var facturasDetalladas = await query
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        // Información de la Factura
                        factura = new
                        {
                            numeroFactura = f.FacNumeroFactura,
                            numeroFormateado = $"{f.FacNumeroFactura.Substring(0, 3)}-{f.FacNumeroFactura.Substring(3, 3)}-{f.FacNumeroFactura.Substring(6, 9)}",
                            fechaEmision = f.FacFechaEmision,
                            estado = f.FacEstadoFactura ? "Activa" : "Anulada",
                            estadoBoolean = f.FacEstadoFactura,
                            subtotalSinIva = f.FacSubtotalSinIva,
                            subtotalConIva = f.FacSubtotalConIva,
                            iva = f.FacIva,
                            total = f.FacTotal,
                            usuarioCreacion = f.FacUsuarioCreacion,
                            fechaCreacion = f.FacFechaCreacion
                        },

                        // Información del Cliente
                        cliente = new
                        {
                            cedula = f.Cliente.CliCedula,
                            nombre = f.Cliente.CliNombre,
                            email = f.Cliente.CliEmail,
                            telefono = f.Cliente.CliTelefono
                        },

                        // Información del Punto de Emisión
                        puntoEmision = new
                        {
                            codigo = f.PuntoEmision.PemIdPuntoEmision,
                            descripcion = f.PuntoEmision.PemDescripcion,
                            establecimiento = f.PuntoEmision.Establecimiento.EstNombre
                        },

                        // Tipo de Pago
                        tipoPago = f.TipoPago.TipDescripcion,

                        // Detalles de Productos
                        productos = f.DetallesFactura.Select(d => new
                        {
                            idDetalle = d.DetIdDetalle,
                            idProductoExterno = d.DetIdProductoExterno,
                            codigoProducto = d.DetCodigoProducto,
                            nombreProducto = d.DetNombreProducto,
                            cantidad = d.DetCantidad,
                            precioUnitario = d.DetPrecioUnitario,
                            subtotal = d.DetSubtotal,
                            grabaIva = d.DetGrabaIva,
                            porcentajeIva = d.DetPorcentajeIva,
                            valorIva = d.DetValorIva,
                            total = d.DetTotal
                        }).ToList(),

                        // Resumen
                        resumen = new
                        {
                            totalProductos = f.DetallesFactura.Count,
                            totalUnidades = f.DetallesFactura.Sum(d => d.DetCantidad ?? 0),
                            productosConIva = f.DetallesFactura.Count(d => d.DetGrabaIva == true),
                            productosSinIva = f.DetallesFactura.Count(d => d.DetGrabaIva == false)
                        }
                    })
                    .ToListAsync();

                // Calcular estadísticas generales
                var estadisticas = new
                {
                    totalFacturas = facturasDetalladas.Count,
                    facturasActivas = facturasDetalladas.Count(f => f.factura.estadoBoolean),
                    facturasAnuladas = facturasDetalladas.Count(f => !f.factura.estadoBoolean),
                    totalVentas = facturasDetalladas
                        .Where(f => f.factura.estadoBoolean)
                        .Sum(f => f.factura.total),
                    totalProductosVendidos = facturasDetalladas
                        .Where(f => f.factura.estadoBoolean)
                        .Sum(f => f.resumen.totalUnidades),
                    promedioVenta = facturasDetalladas
                        .Where(f => f.factura.estadoBoolean)
                        .Average(f => (double?)f.factura.total) ?? 0
                };

                return Ok(new
                {
                    success = true,
                    filtros = new
                    {
                        cliCedula = string.IsNullOrEmpty(cliCedula) ? "Todos los clientes" : cliCedula
                    },
                    estadisticas,
                    data = facturasDetalladas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener el reporte de facturas detalladas",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene todas las facturas que contienen un producto específico
        /// Retorna solo la factura y sus detalles
        /// </summary>
        /// <param name="codigoProducto">Código del producto (ej: PRD-0002)</param>
        /// <returns>Lista de facturas con sus detalles</returns>
        [HttpGet("por-producto/{codigoProducto}")]
        public async Task<ActionResult> GetFacturasPorProducto(string codigoProducto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoProducto))
                {
                    return BadRequest(new { success = false, message = "El código de producto es requerido" });
                }

                var facturas = await _context.Facturas
                    .Include(f => f.DetallesFactura)
                    // 1. Filtro Padre: Selecciona las facturas que contienen el producto
                    .Where(f => f.DetallesFactura.Any(d => d.DetCodigoProducto == codigoProducto))
                    .OrderByDescending(f => f.FacFechaEmision)
                    .Select(f => new
                    {
                        // Datos de Cabecera (Factura)
                        facNumeroFactura = f.FacNumeroFactura,
                        facFechaEmision = f.FacFechaEmision,
                        facTotal = f.FacTotal,
                        // ... puedes dejar el resto de campos de cabecera si los necesitas ...

                        // 2. Filtro Hijo: AQUÍ ESTÁ LA SOLUCIÓN
                        // Solo selecciona los detalles que coinciden con el código buscado
                        detallesFactura = f.DetallesFactura
                            .Where(d => d.DetCodigoProducto == codigoProducto) // <--- FILTRO APLICADO AQUI
                            .Select(d => new
                            {
                                detNombreProducto = d.DetNombreProducto,
                                detCodigoProducto = d.DetCodigoProducto,
                                detCantidad = d.DetCantidad, // <--- Lo que te interesa
                                detTotal = d.DetTotal
                            }).ToList()
                    })
                    .ToListAsync();

                if (!facturas.Any())
                {
                    return Ok(new { success = true, message = $"No se encontraron facturas con el producto '{codigoProducto}'", data = new List<object>() });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Se encontraron {facturas.Count} facturas con el producto '{codigoProducto}'",
                    data = facturas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error", error = ex.Message });
            }
        }





    }
}