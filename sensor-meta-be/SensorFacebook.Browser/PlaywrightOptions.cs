using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Browser
{
    public sealed class PlaywrightOptions
    {
        public bool Headless { get; set; } = true;
        public string? ExecutablePath { get; set; } 
        public int ContextTimeoutMs { get; set; } = 45000;
        public bool Trace { get; set; } = false;
    }
}
