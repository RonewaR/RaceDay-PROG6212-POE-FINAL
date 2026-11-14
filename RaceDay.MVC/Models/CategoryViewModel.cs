using System;

namespace RaceDay.MVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        public string Name { get; set; } = "";

        public double DistanceKm { get; set; }

        public decimal EntryFee { get; set; }

        public DateTime StartTime { get; set; }
    }
}