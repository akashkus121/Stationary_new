using Microsoft.AspNetCore.Mvc;
using Stationary.Services;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventStreamService _eventStream;

        public EventsController(IEventStreamService eventStream)
        {
            _eventStream = eventStream;
        }

        [HttpGet("stream")]
        public async Task StreamEvents(CancellationToken cancellationToken)
        {
            await _eventStream.SubscribeAsync(HttpContext, cancellationToken);
        }
    }
}
