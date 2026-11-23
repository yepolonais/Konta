namespace Konta.Application.Accounts;

public sealed class CommonAccountRequiredException()
    : InvalidOperationException("Désignez d'abord le compte commun dans l'onglet Projection.");
