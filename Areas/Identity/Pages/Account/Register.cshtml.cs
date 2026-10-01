// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace BeeDienLanh.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(
            100,
            ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare(
            "Password",
            ErrorMessage = "Mật khẩu xác nhận không trùng khớp.")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        ExternalLogins =
            (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();
    }

    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        ExternalLogins =
            (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

        if (ModelState.IsValid)
        {
            var user = CreateUser();

            // Gán Email làm Username
            await _userStore.SetUserNameAsync(
                user,
                Input.Email,
                CancellationToken.None);

            await _emailStore.SetEmailAsync(
                user,
                Input.Email,
                CancellationToken.None);

            // Tạo tài khoản
            var result = await _userManager.CreateAsync(
                user,
                Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Người dùng đã tạo tài khoản mới.");

                // =====================================
                // TỰ ĐỘNG GÁN ROLE CUSTOMER
                // =====================================
                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Customer");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return Page();
                }

                // =====================================
                // TẠO TOKEN XÁC NHẬN EMAIL
                // =====================================
                var userId =
                    await _userManager.GetUserIdAsync(user);

                var code =
                    await _userManager
                        .GenerateEmailConfirmationTokenAsync(user);

                code = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId = userId,
                        code = code,
                        returnUrl = returnUrl
                    },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Xác nhận tài khoản Bee Điện Lạnh",
                    $"Vui lòng xác nhận tài khoản bằng cách " +
                    $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
                    $"bấm vào đây</a>.");

                // =====================================
                // KIỂM TRA CÓ YÊU CẦU XÁC NHẬN EMAIL KHÔNG
                // =====================================
                if (_userManager.Options
                    .SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage(
                        "RegisterConfirmation",
                        new
                        {
                            email = Input.Email,
                            returnUrl = returnUrl
                        });
                }
                else
                {
                    // Đăng nhập ngay sau khi đăng ký
                    await _signInManager.SignInAsync(
                        user,
                        isPersistent: false);

                    return LocalRedirect(returnUrl);
                }
            }

            // Hiển thị lỗi nếu tạo tài khoản thất bại
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }
        }

        return Page();
    }

    private IdentityUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser>();
        }
        catch
        {
            throw new InvalidOperationException(
                $"Không thể tạo đối tượng '{nameof(IdentityUser)}'.");
        }
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException(
                "Identity yêu cầu User Store hỗ trợ Email.");
        }

        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}