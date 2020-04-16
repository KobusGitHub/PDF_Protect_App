using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDF_Protect_App
{
    public class Identifier
    {
        public string IdNumber { get; set; }
        public string PersonName { get; set; }

        public Boolean isLoaded
        {
            get
            {
                if (string.IsNullOrEmpty(IdNumber) || string.IsNullOrEmpty(PersonName))
                {
                    return false;
                }

                return true;
            }
        }

    }
}
