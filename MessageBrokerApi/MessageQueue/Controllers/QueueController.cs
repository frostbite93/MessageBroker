using MessageBrokerApi.MessageQueue.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MessageBrokerApi.MessageQueue.Controllers
{
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly IMessageBroker _broker;

        public QueueController(IMessageBroker broker, IConfiguration config)
        {
            _broker = broker;
        }

        [Route("{*url}")]
        public async Task<IActionResult> Enqueue()
        {
            var method = Request.Method;
            var path = Request.Path + Request.QueryString;

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);

            var (statusCode, response) = await _broker.SendAndWaitAsync(method, path, body);
            return StatusCode(statusCode, response);
        }
    }
}
