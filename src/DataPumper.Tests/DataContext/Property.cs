using System;
using System.ComponentModel.DataAnnotations;

namespace DataPumper.Tests
{
    public class Property
    {
        [Key]
        public int Id { get; set; }

        public DateTime CurrentDate { get; set; } = DateTime.Today;
    }
}