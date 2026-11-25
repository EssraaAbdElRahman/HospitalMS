namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class UpdateStatusViewModel
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public AccountStatus CurrentStatus { get; set; }

        [Required(ErrorMessage = "Please select a status.")]
        public AccountStatus? NewStatus { get; set; }
        public List<AccountStatus> AllowedStatuses { get; set; } = new();
    }
}
