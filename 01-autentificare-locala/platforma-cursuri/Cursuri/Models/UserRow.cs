namespace Cursuri.Models;

// Un rând din pagina Users: contul, de unde vine și ce spune registrul secretariatului despre el.
public record UserRow(IdentityRecord Account, string Origin, string SecretariatCheck);
