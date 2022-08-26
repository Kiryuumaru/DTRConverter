using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTRConverter;

public class Dtr
{
    public DateTime DateTime { get; set; }

    public VerifyCode VerifyCode { get; set; }

    public Dtr(DateTime dateTime, VerifyCode verifyCode)
    {
        DateTime = dateTime;
        VerifyCode = verifyCode;
    }
}
