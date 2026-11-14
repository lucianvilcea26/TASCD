using Biblioteca.Accounts;
using Biblioteca.Data;
using Biblioteca.Email;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class AccountController(AccountService accounts, ISecretariatClient secretariat, IEmailSender email,
    ILogger<AccountController> logger) : Controller
{
    // ------------------------------------------------------------ crearea contului

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        foreach (var problem in accounts.CheckPassword(model.Password, model.Email))
            ModelState.AddModelError(nameof(model.Password), problem);
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

        // Dacă emailul are deja cont, nu o spunem pe pagină (oricine ar putea afla astfel cine are cont),
        // ci îi scriem titularului adresei. Pagina arată la fel în ambele cazuri.
        var existing = await accounts.FindByEmailAsync(model.Email);
        if (existing is not null)
        {
            await email.SendAsync(existing.Email, "Ai deja un cont la Biblioteca ASE",
                "Cineva a încercat să creeze un cont nou cu această adresă, dar contul există deja.\n\n" +
                $"Dacă ai uitat parola, o poți schimba aici: {Url.Action(nameof(ForgotPassword), "Account", null, Request.Scheme)}\n\n" +
                "Dacă nu ai fost tu, poți ignora mesajul.");
            return CheckYourEmail();
        }

        var user = await accounts.CreateReaderAsync(model.Email, check.Name!, model.Matricol.Trim(), model.Password);
        await SendConfirmationAsync(user);
        return CheckYourEmail();
    }

    private async Task SendConfirmationAsync(User user)
    {
        var token = await accounts.CreateTokenAsync(user, TokenPurposes.ConfirmEmail);
        var link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, token }, Request.Scheme);
        await email.SendAsync(user.Email, "Confirmă adresa de email pentru Biblioteca ASE",
            $"Bună, {user.Name},\n\nPentru a activa contul de cititor {user.CardNumber}, deschide linkul de mai jos:\n\n{link}\n\n" +
            "Linkul poate fi folosit o singură dată și expiră într-o oră.");
    }

    private ViewResult CheckYourEmail() => View("Message", new MessageViewModel(
        "Verifică emailul",
        "Ți-am trimis un email la adresa din dosarul de la secretariat. Deschide linkul din el pentru a activa contul."));

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId, string token)
    {
        var user = await accounts.FindByIdAsync(userId);
        if (user is not null && await accounts.ConfirmEmailAsync(user, token))
            return View("Message", new MessageViewModel("Adresă confirmată",
                $"Contul de cititor {user.CardNumber} este activ. Te poți autentifica.", "Autentificare", nameof(Login)));

        return View("Message", new MessageViewModel("Linkul nu este valabil",
            "Linkul a fost deja folosit, a expirat sau a fost înlocuit de un link mai nou.",
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

        var user = await accounts.FindByEmailAsync(model.Email);
        if (user is { EmailConfirmed: false })
            await SendConfirmationAsync(user);
        return View("Message", new MessageViewModel("Verifică emailul",
            "Dacă adresa are un cont neconfirmat, ți-am trimis un link nou. Linkurile trimise anterior nu mai funcționează."));
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

        var result = await accounts.LoginAsync(model.Email, model.Password);
        switch (result.Outcome)
        {
            case LoginOutcome.InvalidCredentials:
                // Același mesaj pentru email inexistent și parolă greșită.
                ModelState.AddModelError("", "Adresa de email sau parola nu sunt corecte.");
                return View(model);

            case LoginOutcome.LockedOut:
                ModelState.AddModelError("", $"Prea multe încercări greșite. Contul este blocat până la {result.LockedUntilUtc!.Value.ToLocalTime():HH:mm}.");
                return View(model);

            case LoginOutcome.EmailNotConfirmed:
                ViewData["ShowResend"] = true;
                ModelState.AddModelError("", "Confirmă întâi adresa de email, din linkul primit la crearea contului.");
                return View(model);
        }

        var user = result.User!;
        var enrollment = await secretariat.TryGetEnrollmentAsync(user.Matricol, logger);
        await HttpContext.SignInAsync(UserPrincipal.Create(user, enrollment));
        return Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
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

        var user = (await accounts.FindByIdAsync(User.Id()))!;
        var problems = await accounts.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (problems.Count > 0)
        {
            foreach (var problem in problems)
                ModelState.AddModelError("", problem);
            return View(model);
        }

        // Ștampila s-a schimbat: toate celelalte sesiuni ale contului se închid la următoarea cerere.
        // Pe aceasta o păstrăm, cu un cookie nou, care conține ștampila nouă.
        var enrollment = await secretariat.TryGetEnrollmentAsync(user.Matricol, logger);
        await HttpContext.SignInAsync(UserPrincipal.Create(user, enrollment));
        await email.SendAsync(user.Email, "Parola contului Biblioteca ASE a fost schimbată",
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

        var user = await accounts.FindByEmailAsync(model.Email);
        if (user is { EmailConfirmed: true })
        {
            var token = await accounts.CreateTokenAsync(user, TokenPurposes.ResetPassword);
            var link = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, token }, Request.Scheme);
            await email.SendAsync(user.Email, "Schimbarea parolei la Biblioteca ASE",
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

        var user = int.TryParse(model.UserId, out var id) ? await accounts.FindByIdAsync(id) : null;
        var problems = user is null
            ? ["Linkul nu mai este valabil. Cere un link nou."]
            : await accounts.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (problems.Count > 0)
        {
            foreach (var problem in problems)
                ModelState.AddModelError("", problem);
            return View(model);
        }

        return View("Message", new MessageViewModel("Parolă schimbată",
            "Parola a fost schimbată, iar sesiunile deschise cu parola veche au fost închise.", "Autentificare", nameof(Login)));
    }
}
