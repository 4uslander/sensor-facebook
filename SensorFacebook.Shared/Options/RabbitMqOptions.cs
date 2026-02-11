using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Shared.Options
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "sensor.jobs";
        public string DeadLetterExchange { get; set; } = "sensor.dlx";
    }

}
