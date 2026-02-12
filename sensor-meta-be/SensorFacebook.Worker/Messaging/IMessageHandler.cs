using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Worker.Messaging
{
    public interface IMessageHandler<TMessage>
    {
        Task HandleAsync(TMessage msg, CancellationToken ct);
    }
}
