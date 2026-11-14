using System.Collections.Generic;

namespace RaceDay.MVC.Models
{
    public class ParticipantEventsViewModel
    {
        public List<EventViewModel> Events { get; set; } = new();

        public List<CategoryViewModel> Categories { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;

        public string LocationFilter { get; set; } = string.Empty;
    }
}