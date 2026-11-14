using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        [Required(ErrorMessage = "Please enter the event title.")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Please enter an event description.")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Please select an event date.")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Please enter the event location.")]
        public string Location { get; set; } = "";

        public string ImageUrl { get; set; } = "";
    }
}