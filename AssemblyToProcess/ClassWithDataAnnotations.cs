using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AssemblyToProcess
{
    public class ClassWithDataAnnotations
    {
        [Display(Name = "Last Name", Order = 0)]
        public string LastName { get; set; }

        [Display(Name = "First Name", Order = 1)]
        public string FirstName { get; set; }
    }
}