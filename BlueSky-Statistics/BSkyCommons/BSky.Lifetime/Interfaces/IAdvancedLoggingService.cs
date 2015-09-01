using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSky.Lifetime.Interfaces
{
    public interface IAdvancedLoggingService
    {
        bool AdvancedLog { get; }
        void RefreshAdvancedLogging();
    }
}
