namespace Certifications.Application.Abstractions;

public interface IPasswordProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);

    bool Verify(string plaintext, string protectedValue);
}

public interface IPasswordGenerator
{
    string Generate();
}

public interface IBusinessClock
{
    DateOnly Today { get; }
}
