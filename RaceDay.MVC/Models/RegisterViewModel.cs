namespace RaceDay.MVC.Models
{
    public class RegisterViewModel
    {
        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        public string FullName { get; set; } = "";

        public string Role { get; set; } = "Participant";
    }
}