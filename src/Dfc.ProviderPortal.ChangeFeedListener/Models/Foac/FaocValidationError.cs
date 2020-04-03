using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models.Foac
{
    public class FaocValidationError
    {
        public string ID { get; set; }
        public IList<string> Error { get; set; }
    }
}
