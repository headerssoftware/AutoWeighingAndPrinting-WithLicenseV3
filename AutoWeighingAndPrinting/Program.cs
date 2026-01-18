using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AutoWeighingAndPrinting
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [Obsolete]
        static void Main()
        {
            if (ApplicationRunningHelper.AlreadyRunning()) //Check instance is already running, then set focus
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
