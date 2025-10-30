namespace VisualSoftechBackend.Models
{
    public class StudentMaster
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string? Address { get; set; }
        public int StateId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhotoPath { get; set; }
        public List<StudentDetail>? Subjects { get; set; }
    }
}
