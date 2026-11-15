namespace Cursuri.Models;

public static class Contexts
{
    public const string Secretariat = "Secretariat";
    public const string Cursuri = "Cursuri";

    // Nu este un context, ci un issuer: valoarea a fost declarată chiar de utilizator și nu a verificat-o nimeni.
    public const string SelfAsserted = "Self-asserted";
}
