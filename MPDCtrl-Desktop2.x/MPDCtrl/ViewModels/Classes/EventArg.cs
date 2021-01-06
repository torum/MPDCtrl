using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.ViewModels.Classes
{
    /// <summary>
    /// </summary>
    public class ShowDebugEventArgs : EventArgs
    {
        public bool WindowVisibility = true;
        public double Top = 100;
        public double Left = 100;
        public double Height = 240;
        public double Width = 450;
    }

}
