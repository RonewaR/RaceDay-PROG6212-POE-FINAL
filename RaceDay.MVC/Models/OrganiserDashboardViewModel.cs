namespace RaceDay.MVC.Models
{
    public class OrganiserDashboardViewModel
    {
        public int TotalEvents { get; set; }

        public int TotalEnrolments { get; set; }

        public int UpcomingEvents { get; set; }

        public List<OrganiserEventSummaryViewModel> Events { get; set; }
            = new List<OrganiserEventSummaryViewModel>();
    }

    public class OrganiserEventSummaryViewModel
    {
        public int EventId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public int EnrolmentCount { get; set; }
    }
}