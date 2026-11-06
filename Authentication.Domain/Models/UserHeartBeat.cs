using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Domain.Models
{
    public class UserHeartBeat
    {
        public string UserId { get; set; }
        public DateTime? LastSeen { get; set; }
        //public string? TabId { get; set; }
        //Otomatik Temizlik
        //DELETE FROM dbo.UserHeartbeat WHERE LastSeen < DATEADD(HOUR, -24, GETUTCDATE())
    }
}
