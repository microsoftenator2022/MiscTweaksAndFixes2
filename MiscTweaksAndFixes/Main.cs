using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MicroWrath;

using MiscTweaksAndFixes.Tweaks;

namespace MiscTweaksAndFixes
{
    public partial class Main : IMicroMod
    {
        private Main()
        {
            var initMethods = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Where(m => m.GetParameters().Length == 0 && m.GetCustomAttribute<InitAttribute>() is not null);

            foreach (var method in initMethods)
            {
                method.Invoke(null, null);
            }
        }
    }
}
