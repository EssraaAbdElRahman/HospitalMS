using HospitalMS.DAL.Enums;
using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HospitalMS.PL.Controllers.Home
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (user.Status != AccountStatus.Active)
                return RedirectToAction("AccessDenied", "Account");

            if (user.UserType == UserType.Admin)
                return RedirectToAction("Index", "User");

            if (user.UserType == UserType.Staff)
            {
                if (user.StaffType == StaffType.Doctor)
                    return RedirectToAction("Index", "DoctorDashboard");

                if (user.StaffType == StaffType.Receptionist)
                    return RedirectToAction("Index", "Appointment");
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}