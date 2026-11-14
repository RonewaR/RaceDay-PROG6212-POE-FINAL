using System;

namespace RaceDay.MVC.Models
{
    public class ResultViewModel
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }

        public string EventTitle { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public TimeOnly? FinishTime { get; set; }

        public int? Position { get; set; }

        public bool IsDnf { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}