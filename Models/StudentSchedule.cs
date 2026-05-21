namespace HouseofTutorAPI.Models
{
    public class StudentSchedule
    {
        public int ScheduleId { get; set; }

        public int? StudentId { get; set; }

        public string? Day { get; set; }

        public string? Time { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? Type { get; set; }

        public virtual Student? Student { get; set; }
    }
}
