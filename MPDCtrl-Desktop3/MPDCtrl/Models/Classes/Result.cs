using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Models
{
    public class Result
    {
        public bool IsSuccess;
        public string ErrorMessage;
    }

    public class ConnectionResult: Result
    {

    }

    public class CommandResult : Result
    {
        public string ResultText;
    }

}
