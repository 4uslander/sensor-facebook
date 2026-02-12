using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Shared.Abstractions
{
    public interface IBusPublisher
    {
        Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default);
    }

}
