using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("top-customers")]
        public async Task<ActionResult<List<TopCustomerReportDto>>> GetTopCustomers([FromQuery] int days = 30, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            var result = await _reportService.GetTopCustomersAsync(days, limit, cancellationToken);
            return Ok(result);
        }
    }
}