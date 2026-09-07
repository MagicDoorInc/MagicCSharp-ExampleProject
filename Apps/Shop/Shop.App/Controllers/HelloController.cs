using Microsoft.AspNetCore.Mvc;

namespace Acme.Shop.App.Controllers;

/// <summary>
///     Delete this once the service has a real endpoint. It is here so a freshly created service runs and
///     answers something.
/// </summary>
[ApiController]
[Route("hello")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { service = "Shop" });
    }
}
