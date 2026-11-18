using Microsoft.AspNetCore.Mvc;

namespace QuickSign.HttpApi.Controllers;

/// <summary>
/// Abstract base controller establishing common API routing conventions and behaviors.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class QuickSignControllerBase : ControllerBase
{
}
