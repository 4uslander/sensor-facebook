using SensorFacebook.Shared.Messaging;
using SensorFacebook.Worker.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Worker.Handlers
{
    public sealed class NotifyHandler : IMessageHandler<NotifyMsg>
    {
        public Task HandleAsync(NotifyMsg message, CancellationToken ct = default)
        {
            // TODO: implement
            return Task.CompletedTask;
        }
    }
}
