using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RxFair.Data.DbContext;
using RxFair.Data.DbModel;
using RxFair.Dto.Dtos;
using RxFair.Dto.Enum;
using RxFair.Service.Exceptions;
using RxFair.Service.Interface;
using RxFair.Utility;
using RxFair.Utility.Common;

namespace RxFair.Areas.Admin.Controllers
{
    [Authorize(Roles = AuthorizeRoles.Admin), Area("Admin")]
    public class MyAccountController : BaseController<MyAccountController>
    {
        #region Fields
        private readonly IUserService _user;
        private readonly IUserAddressService _userAddress;
        private readonly IStateService _state;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        #endregion

        #region ctor
        public MyAccountController(IUserService user, IUserAddressService userAddress, IStateService state, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _user = user;
            _userAddress = userAddress;
            _state = state;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        #endregion

        #region Methods
        public IActionResult Index()
        {
            BindDropdownList();
            var result = _user.GetSingle(x => x.Id == User.GetUserId());
            var address = result.UserAddress?.FirstOrDefault();
            var dtoModel = new UserProfileDto
            {
                Id = result.Id,
                FirstName = result.FirstName,
                LastName = result.LastName,
                Email = result.Email,
                Mobile = result.Mobile,
                JobTitle = result.JobTitle,
                City = address?.City ?? "",
                StateId = address?.StateId ?? 0,
                ZipCode = address?.ZipCode ?? "",
                UserProfileImage = result.UserProfileImage
            };

            return View(dtoModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(UserProfileDto model)
        {
            using (var txscope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        txscope.Dispose();
                        return JsonResponse.GenerateJsonResult(0, string.Join(",", ModelState.GetModelError()));
                    }
                    var result = await _userManager.FindByIdAsync(model.Id.ToString());
                    result.FirstName = model.FirstName;
                    result.LastName = model.LastName;
                    result.Mobile = model.Mobile;
                    result.JobTitle = model.JobTitle;
                    await _user.UpdateAsync(result, Accessor, model.Id);

                    var address = result.UserAddress?.FirstOrDefault();
                    if (address != null)
                    {
                        address.City = model.City;
                        address.StateId = model.StateId;
                        address.ZipCode = model.ZipCode;
                        address.CreatedBy = result.Id;
                        await _userAddress.UpdateAsync(address, Accessor, result.Id);
                    }
                    else
                    {
                        address = new UserAddress
                        {
                            City = model.City,
                            StateId = model.StateId,
                            ZipCode = model.ZipCode,
                            CreatedBy = result.Id,
                            IsActive = true
                        };
                        await _userAddress.InsertAsync(address, Accessor, result.Id);
                    }

                    txscope.Complete();
                    return JsonResponse.GenerateJsonResult(1, "Your profile information updated successfully.");

                }
                catch (Exception ex)
                {
                    txscope.Dispose();
                    ErrorLog.AddErrorLog(ex, "Post/MyProfile");
                    return JsonResponse.GenerateJsonResult(0, GlobalConstant.SomethingWrong);
                }
            }

        }

        public async Task<IActionResult> ChangePassword(UserProfileDto model)
        {
            using (var txscope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        txscope.Dispose();
                        return JsonResponse.GenerateJsonResult(0, string.Join(",", ModelState.GetModelError()));
                    }
                    var currUser = await _userManager.FindByIdAsync(User.GetUserId().ToString());

                    var result = await _userManager.ChangePasswordAsync(currUser, model.OldPassword, model.NewPassword);
                    if (!result.Succeeded)
                    {
                        //AddErrors(result);
                        var a = result.Errors.FirstOrDefault();
                        if (a?.Code == "PasswordMismatch")
                        {
                            ModelState.AddModelError(string.Empty, "Old password is incorrect.");
                        }
                        return JsonResponse.GenerateJsonResult(0, string.Join(",", ModelState.GetModelError()));
                    }
                    await _signInManager.SignInAsync(currUser, isPersistent: false);

                    txscope.Complete();
                    return JsonResponse.GenerateJsonResult(1, "Password changed successfully.");

                }
                catch (Exception ex)
                {
                    txscope.Dispose();
                    ErrorLog.AddErrorLog(ex, "Post/ChangePassword");
                    return JsonResponse.GenerateJsonResult(0, GlobalConstant.SomethingWrong);
                }
            }

        }

        public async Task<IActionResult> UploadProfile(IFormFile profileImage)
        {
            using (var txscope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                string newProfileFile = string.Empty;
                try
                {
                    if (profileImage == null)
                    {
                        txscope.Dispose();
                        return JsonResponse.GenerateJsonResult(1, "Profile changed successfully.");
                    }
                    newProfileFile = CommonMethod.GetFileName(profileImage.FileName);
                    await CommonMethod.UploadFileAsync(HostingEnvironment.WebRootPath, FilePathList.UserProfile, newProfileFile, profileImage);
                    var user = await _userManager.FindByIdAsync(User.GetUserId().ToString());
                    var oldProfile = user.UserProfileImage;
                    user.UserProfileImage = newProfileFile;
                    await _user.UpdateAsync(user, Accessor);

                    await _signInManager.RefreshSignInAsync(user);
                    txscope.Complete();

                    if (!string.IsNullOrEmpty(oldProfile))
                    {
                        CommonMethod.DeleteFile(CommonMethod.CheckServerPath(HostingEnvironment.WebRootPath, FilePathList.UserProfile, oldProfile), true);
                    }
                    return JsonResponse.GenerateJsonResult(1, "Profile changed successfully.", $@"\{FilePathList.UserProfile}\{newProfileFile}");
                }
                catch (Exception ex)
                {
                    txscope.Dispose();
                    if (!string.IsNullOrEmpty(newProfileFile))
                    {
                        CommonMethod.DeleteFile(CommonMethod.CheckServerPath(HostingEnvironment.WebRootPath, FilePathList.UserProfile, newProfileFile), true);
                    }
                    ErrorLog.AddErrorLog(ex, "UploadProfile");
                    return JsonResponse.GenerateJsonResult(1, GlobalConstant.SomethingWrong);
                }
            }
        }
        #endregion

        #region Controller Common
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private void BindDropdownList()
        {
            ViewBag.StateList = _state.GetAll().Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).OrderBy(x => x.Text).ToList();
        }
        #endregion

    }
}