using DTPortal.Core.Domain.Services.Communication;

using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

namespace DTPortal.Core.Utilities

{
    public interface IMessageLocalizer

    {
        string GetMessage(LocalizedMessage message);

    }

}

