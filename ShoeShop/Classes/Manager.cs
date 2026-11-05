using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoeShop.Classes
{
    internal class Manager
    {
        public static int IdUser { get; set; }
        public static string FullName { get; set; }
        public static string Login { get; set; }
        public static bool IsAuthorized => !string.IsNullOrEmpty(Login);
    }
}
