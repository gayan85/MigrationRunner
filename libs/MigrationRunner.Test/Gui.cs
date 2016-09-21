using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MigrationRunner.Helpers;
using NUnit.Framework;

namespace MigrationRunner.Test
{
    [TestFixture]
    public class Gui
    {
        [Test]
        public void Form_Controls_Enable()
        {
            var main = new Main();
            Assert.DoesNotThrow(() =>
            {
                main.Enable(true);
            });
        }

        [Test]
        public void Form_Controls_Disabled()
        {
            var main = new Main();
            Assert.DoesNotThrow(() =>
            {
                main.Enable(false);
            });
        }
    }
}
