using Biblioteca.Accounts;
using Biblioteca.Data;
using Biblioteca.Email;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
    ISecretariatClient secretariat, IEmailSender email, ILogger<AccountController> logger) : Controller
{
    // ------------------------------------------------------------ crearea contului

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Regulile pentru parolă (PasswordOptions și EmailNotInPasswordValidator), verificate înainte de secretariat.
        var candidate = new ApplicationUser { UserName = model.Email, Email = model.Email };
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, candidate, model.Password);
            foreach (var error in result.Errors)
                ModelState.AddModelError(nameof(model.Password), error.Description);
        }
        if (!ModelState.IsValid)
            return View(model);

        // Dovedirea identității: nu credem ce scrie studentul în formular, ci întrebăm secretariatul
        // dacă adresa este cea din dosarul acestui număr matricol. Emailul de confirmare merge apoi doar acolo.
        EmailCheckResult check;
        try
        {
            check = await secretariat.CheckEmailAsync(model.Matricol.Trim(), model.Email);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Secretariatul nu a răspuns la crearea unui cont");
            ModelState.AddModelError("", "Secretariatul nu răspunde acum, așa că nu putem verifica datele. Încearcă mai târziu.");
            return View(model);
        }
        if (!check.Matches || !check.Enrolled)
        {
            ModelState.AddModelError("", "Secretariatul nu confirmă un student înmatriculat cu acest număr matricol și această adresă de email.");
            return View(model);
        }

        // Dacă emailul are deja cont, nu o spunem pe pagină, ci îi scriem titularului adresei.
        var existing = await users.FindByEmailAsync(model.Email);
        if (existing is not null)
        {
            await email.SendAsync(existing.Email!, "Ai deja un cont la Biblioteca ASE",
                "Cineva a încercat să creeze un cont nou cu această adresă, dar contul există deja.\n\n" +
                $"Dacă ai uitat parola, o poți schimba aici: {Url.Action(nameof(ForgotPassword), "Account", null, Request.Scheme)}\n\n" +
                "Dacă nu ai fost tu, poți ignora mesajul.");
            return CheckYourEmail();
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim().ToLowerInvariant(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Name = check.Name!,
            Matricol = model.Matricol.Trim(),
            CardNumber = $"B-{await users.Users.CountAsync() + 1:D4}",
        };
        // CreateAsync verifică din nou parola, calculează amprenta ei și salvează contul.
        var created = await users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await users.AddToRoleAsync(user, Roles.Reader);
        await SendConfirmationAsync(user);
        return CheckYourEmail();
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        // Jetonul este produs de DataProtectorTokenProvider: date protejate criptografic (scop, utilizator,
        // ștampila de securitate, momentul emiterii), nu o valoare păstrată în bază.
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, token }, Request.Scheme);
        await email.SendAsync(user.Email!, "Confirmă adresa de email pentru Biblioteca ASE",
            $"Bună, {user.Name},\n\nPentru a activa contul de cititor {user.CardNumber}, deschide linkul de mai jos:\n\n{link}\n\n" +
            "Linkul poate fi folosit o singură dată și expiră într-o oră.");
    }

    private ViewResult CheckYourEmail() => View("Message", new MessageViewModel(
        "Verifică emailul",
        "Ți-am trimis un email la adresa din dosarul de la secretariat. Deschide linkul din el pentru a activa contul."));

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await users.FindByIdAsync(userId);

        // Un jeton de confirmare din Identity nu este, prin el însuși, de unică folosință: ar fi acceptat din nou
        // până la expirare. Tratăm noi un cont deja confirmat ca pe un link folosit.
        if (user is { EmailConfirmed: false } && (await users.ConfirmEmailAsync(user, token)).Succeeded)
            return View("Message", new MessageViewModel("Adresă confirmată",
                $"Contul de cititor {user.CardNumber} este activ. Te poți autentifica.", "Autentificare", nameof(Login)));

        return View("Message", new MessageViewModel("Linkul nu este valabil",
            "Linkul a fost deja folosit sau a expirat.",
            "Trimite din nou emailul de confirmare", nameof(ResendConfirmation)));
    }

    [HttpGet]
    public IActionResult ResendConfirmation() => View(new EmailViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(EmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await users.FindByEmailAsync(model.Email);
        if (user is { EmailConfirmed: false })
            await SendConfirmationAsync(user);
        return View("Message", new MessageViewModel("Verifică emailul",
            "Dacă adresa are un cont neconfirmat, ți-am trimis un link nou."));
    }

    // ------------------------------------------------------------ autentificarea

    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await users.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Identity nu egalizează singur durata răspunsului pentru un email inexistent; o facem noi,
            // verificând o parolă oarecare, ca durata să nu spună cine are cont.
            users.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), DummyHash, model.Password);
            ModelState.AddModelError("", "Adresa de email sau parola nu sunt corecte.");
            return View(model);
        }

        // lockoutOnFailure: true numără încercările greșite și blochează contul după MaxFailedAccessAttempts.
        var result = await signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", $"Prea multe încercări greșite. Contul este blocat până la {user.LockoutEnd!.Value.ToLocalTime():HH:mm}.");
            return View(model);
        }
        if (result.IsNotAllowed)
        {
            // Atenție: Identity verifică emailul confirmat ÎNAINTE de parolă, deci acest mesaj apare chiar și
            // pentru o parolă greșită. Varianta proprie verifică întâi parola.
            ViewData["ShowResend"] = true;
            ModelState.AddModelError("", "Confirmă întâi adresa de email, din linkul primit la crearea contului.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Adresa de email sau parola nu sunt corecte.");
            return View(model);
        }

        await SignInWithEnrollmentAsync(user);
        return Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    private static readonly string DummyHash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), "parola-care-nu-se-potriveste");

    // Deschide sesiunea cu afirmațiile din bază (BibliotecaClaimsFactory) plus răspunsul de acum al secretariatului.
    private async Task SignInWithEnrollmentAsync(ApplicationUser user)
    {
        var enrollment = await secretariat.TryGetEnrollmentAsync(user.Matricol, logger);
        await signIn.SignInWithClaimsAsync(user, isPersistent: false,
            enrollment is null ? [] : [UserClaims.Enrollment(enrollment)]);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public IActionResult Me() => View(User.Claims);

    public IActionResult AccessDenied() => View();

    // ------------------------------------------------------------ schimbarea parolei

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = (await users.GetUserAsync(User))!;
        // ChangePasswordAsync verifică parola curentă, aplică regulile pentru parola nouă și schimbă ștampila.
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        // Celelalte sesiuni au ștampila veche și se închid la următoarea cerere; pe aceasta o redeschidem.
        await SignInWithEnrollmentAsync(user);
        await email.SendAsync(user.Email!, "Parola contului Biblioteca ASE a fost schimbată",
            "Parola contului tău a fost schimbată. Dacă nu ai fost tu, folosește imediat „Am uitat parola\".");
        return View("Message", new MessageViewModel("Parolă schimbată",
            "Parola a fost schimbată. Sesiunile deschise pe alte dispozitive au fost închise."));
    }

    // ------------------------------------------------------------ parola uitată

    [HttpGet]
    public IActionResult ForgotPassword() => View(new EmailViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(EmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await users.FindByEmailAsync(model.Email);
        if (user is { EmailConfirmed: true })
        {
            // Jetonul include ștampila de securitate: devine invalid imediat ce parola se schimbă.
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, token }, Request.Scheme);
            await email.SendAsync(user.Email!, "Schimbarea parolei la Biblioteca ASE",
                $"Pentru a alege o parolă nouă, deschide linkul de mai jos:\n\n{link}\n\n" +
                "Linkul poate fi folosit o singură dată și expiră într-o oră. Dacă nu ai cerut schimbarea, ignoră mesajul: parola rămâne aceeași.");
        }

        // Același răspuns, indiferent dacă adresa are cont sau nu.
        return View("Message", new MessageViewModel("Verifică emailul",
            "Dacă există un cont activ cu această adresă, ți-am trimis un link pentru alegerea unei parole noi."));
    }

    [HttpGet]
    public IActionResult ResetPassword(string userId, string token) =>
        View(new ResetPasswordViewModel { UserId = userId, Token = token });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await users.FindByIdAsync(model.UserId);
        // ResetPasswordAsync verifică jetonul și parola nouă; o parolă respinsă nu schimbă ștampila,
        // deci linkul rămâne folosibil. După succes, ștampila nouă face linkul de nefolosit.
        var result = user is null
            ? IdentityResult.Failed(users.ErrorDescriber.InvalidToken())
            : await users.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        // Identity nu ridică singur blocarea la resetare; o facem, ca în varianta proprie.
        await users.SetLockoutEndDateAsync(user!, null);
        await users.ResetAccessFailedCountAsync(user!);
        return View("Message", new MessageViewModel("Parolă schimbată",
            "Parola a fost schimbată, iar sesiunile deschise cu parola veche au fost închise.", "Autentificare", nameof(Login)));
    }
}
