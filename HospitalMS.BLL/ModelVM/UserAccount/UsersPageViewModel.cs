namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class UsersPageViewModel
    {
        public GetUsersIndexInfoViewModel Info { get; set; } = new();

        public UsersIndexViewModel Users { get; set; } = new();
    }
}
