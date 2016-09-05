using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MigrationRunner.Helpers
{
    public static class HelperExtensions
    {
        public static void Enable(this Control con, bool enable)
        {
            if (con == null) return;
            foreach (Control c in con.Controls) c.Enable(enable);
            try
            {
                con.Invoke((MethodInvoker)(() => con.Enabled = enable));
            }
            catch
            {
                // ignored
            }
        }
    }
}
