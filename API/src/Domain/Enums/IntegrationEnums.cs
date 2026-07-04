namespace VSky.Domain.Enums;

/// <summary>
/// Input data type of a <see cref="Entities.CredentialDefinition"/> field. Drives validation and the
/// auto-generated admin credential form (Credential Vault blueprint, WO-7). Secrecy is a separate flag.
/// </summary>
public enum CredentialFieldType
{
    String = 0,
    Number = 1,
    Boolean = 2,
    Url = 3,
}
