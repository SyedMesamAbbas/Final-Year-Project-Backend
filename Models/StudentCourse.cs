namespace HouseofTutorAPI.Models
{
    public class StudentCourse
    {
        public int StudentId { get; set; }

        public int CourseId { get; set; }

        public virtual Course Course { get; set; } = null!;

        public virtual Student Student { get; set; } = null!;
    }
}
