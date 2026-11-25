using HospitalMS.BLL.Helpers;
using HospitalMS.BLL.ModelVM.Doctor;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Receptionist;
using HospitalMS.BLL.ModelVM.UserAccount;
using HospitalMS.DAL.Database;
using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalMS.BLL.Service.Implementation
{
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IDoctorService _doctorService;
        private readonly IReceptionistService _receptionistService;
        private readonly HospitalDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public UserAccountService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager,
            IDoctorService doctorService, IReceptionistService receptionistService, HospitalDbContext context, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _doctorService = doctorService;
            _receptionistService = receptionistService;
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult> CreateAsync(            CreateUserViewModel viewModel,            string createdByAdminId,            CancellationToken ct)
        {
            if (viewModel == null)
            {
                return ServiceResult.Failure("ViewModel cannot be null.");
            }

            UpdateDoctorViewModel? doctor = null;
            UpdateReceptionistViewModel? receptionist = null;

            bool isDoctor = viewModel.UserType == UserType.Staff &&
                            viewModel.StaffType == StaffType.Doctor;
            bool isReceptionist = viewModel.UserType == UserType.Staff &&
                                  viewModel.StaffType == StaffType.Receptionist;

            if (isDoctor)
            {
                var validDoctor = await ValidateDoctor(viewModel.DoctorId ?? Guid.Empty, ct);
                if (!validDoctor.Succeeded)
                {
                    return validDoctor;
                }
                doctor = validDoctor.Data!;
            }

            if (isReceptionist)
            {
                var validReceptionist = await ValidateReceptionist(viewModel.ReceptionistId ?? Guid.Empty, ct);
                if (!validReceptionist.Succeeded)
                {
                    return validReceptionist;
                }
                receptionist = validReceptionist.Data!;
            }

            var validationResult = await ValidateAccount(viewModel);
            if (!validationResult.Succeeded)
            {
                return validationResult;
            }

            var roleValidationResult = await ValidateRole(viewModel.Role);
            if (!roleValidationResult.Succeeded)
            {
                return roleValidationResult;
            }

            var role = roleValidationResult.Data!;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var user = ApplicationUser.CreateStaff(
                    viewModel.FullName,
                    viewModel.Email,
                    viewModel.UserType,
                    viewModel.StaffType,
                    createdByAdminId);

                var temporaryPassword = $"Tmp@{Guid.NewGuid():N}A1";

                var result = await _userManager.CreateAsync(user, temporaryPassword);
                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure(result.Errors.Select(e => e.Description));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, role.Name!);
                if (!roleResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure(roleResult.Errors.Select(e => e.Description));
                }

                if (isDoctor)
                {
                    var assignResult = await _doctorService.AssignUserAsync(
                        doctor!.Id, user.Id, createdByAdminId, ct);

                    if (!assignResult.Succeeded)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult.Failure(assignResult.Errors);
                    }
                }

                if (isReceptionist)
                {
                    var assignResult = await _receptionistService.AssignUserAsync(
                        receptionist!.Id, user.Id, createdByAdminId, ct);

                    if (!assignResult.Succeeded)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult.Failure(assignResult.Errors);
                    }
                }

                await _unitOfWork.CommitTransactionAsync(ct);
                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return ServiceResult.Failure("An error occurred while creating the user.");
            }
        }

        private async Task<ServiceResult<ApplicationRole>> ValidateRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return ServiceResult<ApplicationRole>.Failure("Role not found.");
            }
            if (string.IsNullOrEmpty(role.Name))
            {
                return ServiceResult<ApplicationRole>.Failure("Role has no valid name.");
            }
            return ServiceResult<ApplicationRole>.Success(role);
        }

        private async Task<ServiceResult<UpdateDoctorViewModel>> ValidateDoctor(Guid doctorId, CancellationToken ct)
        {
            var doctor = await _doctorService.GetByIdAsync(doctorId, ct);
            if (doctor == null)
            {
                return ServiceResult<UpdateDoctorViewModel>.Failure("Doctor not found.");
            }
            return ServiceResult<UpdateDoctorViewModel>.Success(doctor);
        }

        private async Task<ServiceResult<UpdateReceptionistViewModel>> ValidateReceptionist(Guid receptionistId, CancellationToken ct)
        {
            var receptionist = await _receptionistService.GetByIdAsync(receptionistId, ct);
            if (receptionist == null)
            {
                return ServiceResult<UpdateReceptionistViewModel>.Failure("Receptionist not found.");
            }
            return ServiceResult<UpdateReceptionistViewModel>.Success(receptionist);
        }

        private async Task<ServiceResult> ValidateAccount(CreateUserViewModel viewModel)
        {
            if (await _userManager.FindByEmailAsync(viewModel.Email) != null)
            {
                return ServiceResult.Failure("Email already exists.");
            }
            return ServiceResult.Success();
        }

        private async Task<ServiceResult<ApplicationUser>> ValidateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<ApplicationUser>.Failure("User not found.");
            }
            return ServiceResult<ApplicationUser>.Success(user);
        }

        public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "N/A";

            string? createdByName = null;
            if (!string.IsNullOrEmpty(user.CreatedByAdminId))
            {
                var creator = await _userManager.FindByIdAsync(user.CreatedByAdminId);
                createdByName = creator?.FullName ?? "Unknown";
            }

            string? updatedByName = null;
            if (!string.IsNullOrEmpty(user.UpdatedByAdminId))
            {
                var updater = await _userManager.FindByIdAsync(user.UpdatedByAdminId);
                updatedByName = updater?.FullName ?? "Unknown";
            }

            return new UserDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                UserType = user.UserType,
                StaffType = user.StaffType,
                RoleName = roleName,
                Status = user.Status,
                PasswordResetStatus = user.PasswordResetStatus,
                CreatedAtUtc = user.CreatedAtUtc,
                CreatedByName = createdByName,
                UpdatedAtUtc = user.UpdatedAtUtc,
                UpdatedByName = updatedByName
            };
        }

        public async Task<UsersSummaryViewModel> GetUsersSummaryAsync(CancellationToken ct)
        {
            var totalUsers = await _userManager.Users.CountAsync(ct);

            var typeCounts = await _userManager.Users
                .GroupBy(u => u.UserType)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var staffTypesCounts = await _userManager.Users
                .Where(u => u.UserType == UserType.Staff && u.StaffType.HasValue)
                .GroupBy(u => u.StaffType!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var accountStatusCounts = await _userManager.Users
                .GroupBy(u => u.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var pendingResetCount = await _userManager.Users
                .CountAsync(u => u.PasswordResetStatus == PasswordResetRequestStatus.Pending, ct);

            return new UsersSummaryViewModel
            {
                TotalUsers = totalUsers,
                AdminCount = typeCounts.GetValueOrDefault(UserType.Admin, 0),
                StaffCount = typeCounts.GetValueOrDefault(UserType.Staff, 0),
                PatientCount = typeCounts.GetValueOrDefault(UserType.Patient, 0),
                DoctorCount = staffTypesCounts.GetValueOrDefault(StaffType.Doctor, 0),
                ReceptionistCount = staffTypesCounts.GetValueOrDefault(StaffType.Receptionist, 0),
                TotalActive = accountStatusCounts.GetValueOrDefault(AccountStatus.Active, 0),
                TotalUserPending = accountStatusCounts.GetValueOrDefault(AccountStatus.Pending, 0),
                TotalPasswordResetRequests = pendingResetCount
            };
        }

        public async Task<PagedResult<UsersListItemViewModel>> GetPagedAsync(
            GetUsersIndexInfoViewModel viewModel, CancellationToken ct)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(viewModel.Search))
            {
                query = query.Where(u => u.FullName.Contains(viewModel.Search)
                    || u.Email!.Contains(viewModel.Search));
            }
            if (viewModel.UserType.HasValue)
            {
                query = query.Where(u => u.UserType == viewModel.UserType.Value);
            }
            if (viewModel.StaffType.HasValue)
            {
                query = query.Where(u => u.StaffType == viewModel.StaffType.Value);
            }
            if (viewModel.Status.HasValue)
            {
                query = query.Where(u => u.Status == viewModel.Status.Value);
            }
            if (viewModel.PasswordResetStatus.HasValue)
            {
                query = query.Where(u => u.PasswordResetStatus == viewModel.PasswordResetStatus.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var pagedUsers = await query
                .OrderByDescending(u => u.CreatedAtUtc)
                .Skip((viewModel.Page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .Select(u => new { u.Id, u.FullName, u.Email, u.Status, u.PasswordResetStatus })
                .ToListAsync(ct);

            var userIds = pagedUsers.Select(u => u.Id).ToList();

            var userRoles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name }
            ).ToListAsync(ct);

            var rolesByUserId = userRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First().RoleName ?? "N/A");

            var items = pagedUsers.Select(user => new UsersListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                RoleName = rolesByUserId.GetValueOrDefault(user.Id, "N/A"),
                PasswordResetStatus = user.PasswordResetStatus,
                Status = user.Status
            }).ToList();

            return new PagedResult<UsersListItemViewModel>
            {
                Items = items,
                CurrentPage = viewModel.Page,
                PageSize = viewModel.PageSize,
                TotalCount = totalCount,
                SearchTerm = viewModel.Search,
            };
        }

        public async Task<UsersIndexViewModel> GetUsersIndexAsync(
            GetUsersIndexInfoViewModel viewModel, CancellationToken ct)
        {
            var summary = await GetUsersSummaryAsync(ct);
            var pagedUsers = await GetPagedAsync(viewModel, ct);

            return new UsersIndexViewModel
            {
                Summary = summary,
                PagedUsers = pagedUsers,
                SelectedUserType = viewModel.UserType,
                SelectedStaffType = viewModel.StaffType,
                SelectedStatus = viewModel.Status,
                SelectedPasswordResetStatus = viewModel.PasswordResetStatus
            };
        }

        public async Task<ServiceResult> EditUserAsync(
            EditUserViewModel viewModel,
            string updatedByAdminId,
            CancellationToken ct)
        {
            var validationResult = await ValidateUser(viewModel.Id);
            if (!validationResult.Succeeded)
            {
                return validationResult;
            }

            var user = validationResult.Data!;

            var roleValidationResult = await ValidateRole(viewModel.Role);
            if (!roleValidationResult.Succeeded)
            {
                return roleValidationResult;
            }

            var role = roleValidationResult.Data!;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                user.UpdateProfile(viewModel.FullName, updatedByAdminId);

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure(updateResult.Errors.Select(e => e.Description));
                }

                var currentRoles = await _userManager.GetRolesAsync(user);

                if (!currentRoles.Contains(role.Name!))
                {
                    if (currentRoles.Any())
                    {
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return ServiceResult.Failure(removeResult.Errors.Select(e => e.Description));
                        }
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, role.Name!);
                    if (!roleResult.Succeeded)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult.Failure(roleResult.Errors.Select(e => e.Description));
                    }
                }

                await _unitOfWork.CommitTransactionAsync(ct);
                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return ServiceResult.Failure("An error occurred while editing the user.");
            }
        }

        public async Task<EditUserViewModel?> GetByIdAsync(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRoleName = currentRoles.FirstOrDefault();

            string? currentRoleId = null;
            if (currentRoleName is not null)
            {
                var role = await _roleManager.FindByNameAsync(currentRoleName);
                currentRoleId = role?.Id;
            }

            return new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Role = currentRoleId ?? string.Empty
            };
        }

        public async Task<ServiceResult<UpdateStatusViewModel>> GetForStatusUpdateAsync(
            string userId, CancellationToken ct)
        {
            var userValid = await ValidateUser(userId);
            if (!userValid.Succeeded)
            {
                return ServiceResult<UpdateStatusViewModel>.Failure(userValid.Errors);
            }
            var user = userValid.Data!;

            if (user.Status == AccountStatus.Pending)
            {
                return ServiceResult<UpdateStatusViewModel>.Failure(
                    "Pending accounts must be activated via the activation link, not this page.");
            }

            return ServiceResult<UpdateStatusViewModel>.Success(new UpdateStatusViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                CurrentStatus = user.Status,
                AllowedStatuses = AccountStatusHelper.GetAllowedTransitions(user.Status).ToList()
            });
        }

        public async Task<ServiceResult> UpdateStatusAsync(
            string userId,
            AccountStatus newStatus,
            string requestingAdminId,
            CancellationToken ct)
        {
            var userValid = await ValidateUser(userId);
            if (!userValid.Succeeded)
            {
                return ServiceResult.Failure(userValid.Errors);
            }
            var user = userValid.Data!;

            if (!AccountStatusHelper.CanTransitionTo(user.Status, newStatus))
            {
                return ServiceResult.Failure(
                    $"Cannot change status from {user.Status} to {newStatus}.");
            }

            user.SetAccountStatus(newStatus, requestingAdminId);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure(result.Errors.Select(e => e.Description));
            }
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<GenerateInvitationLinkResultViewModel>> GenerateActivationLinkAsync(
            string userId, CancellationToken ct)
        {
            return await GenerateTokenLinkAsync(
                userId,
                user => user.Status == AccountStatus.Pending,
                "Activation link can only be generated for pending accounts.");
        }

        public async Task<ServiceResult<GenerateInvitationLinkResultViewModel>> GeneratePasswordResetLinkAsync(
            string userId, CancellationToken ct)
        {
            return await GenerateTokenLinkAsync(
                userId,
                user => user.PasswordResetStatus == PasswordResetRequestStatus.Pending,
                "There is no pending password reset request for this account.");
        }

        private async Task<ServiceResult<GenerateInvitationLinkResultViewModel>> GenerateTokenLinkAsync(
            string userId,
            Func<ApplicationUser, bool> isEligible,
            string ineligibleMessage)
        {
            var userValid = await ValidateUser(userId);
            if (!userValid.Succeeded)
            {
                return ServiceResult<GenerateInvitationLinkResultViewModel>.Failure(userValid.Errors);
            }
            var user = userValid.Data!;

            if (!isEligible(user))
            {
                return ServiceResult<GenerateInvitationLinkResultViewModel>.Failure(new[] { ineligibleMessage });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return ServiceResult<GenerateInvitationLinkResultViewModel>.Success(
                new GenerateInvitationLinkResultViewModel
                {
                    UserId = user.Id,
                    Token = token
                });
        }
    }
}