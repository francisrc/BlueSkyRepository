using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSky.Lifetime.Interfaces;

namespace BSky.Lifetime.Services
{

    public class AdvancedLoggingService : IAdvancedLoggingService
    {
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();
        public static bool AdvLog;

        public bool AdvancedLog
        {
            get { return AdvLog; }
        }

        public void RefreshAdvancedLogging() // refreshes and gets the new(latest) value set.
        {
            string advlog = confService.AppSettings.Get("advancedlogging");
            // load default value if no value is set 
            if (advlog.Trim().Length == 0)
                advlog = confService.DefaultSettings["advancedlogging"];
            AdvLog = advlog.ToLower().Equals("true") ? true : false;
        }


    }
}
