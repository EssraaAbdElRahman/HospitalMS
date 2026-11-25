namespace HospitalMS.BLL.ModelVM.Receptionist
{
    public class ReceptionistDetailsViewModel
    {
        public Guid Id { get; set; }

        public string? UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;

        public DateTime HireDate { get; set; }

        public decimal Salary { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}