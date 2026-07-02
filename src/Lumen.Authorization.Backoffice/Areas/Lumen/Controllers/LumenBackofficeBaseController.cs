using Lumen.Authorization.Backoffice.Internal;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;

[Area(BackofficeRouteDefaults.AreaName)]
public abstract class LumenBackofficeBaseController : Controller;
