using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2Server
{
    internal enum RequestType
    {
        RequestCommand,
        SendResult,
        DownloadCommand,
        DownloadCommandAndSendData
    }
}
