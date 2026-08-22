using CloneEbay.API.Models;
using CloneEbay.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CategoryResponse>>>> Get(CancellationToken cancellationToken)
    {
        var result = await productService.GetCategoriesAsync(cancellationToken);
        return Ok(ApiEnvelope<IReadOnlyList<CategoryResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
