using Account.Domain.Entities;
using MediatR;

namespace Account.Domain.Events;

public sealed class UserCreatedDomainEvent : INotification
{
    public Guid UserId { get; }
    public string Email { get; }
    public string? PhoneNumber { get; }
    public bool IsEmailConfirmed { get; }
    public string? Name { get; }
    public string? Surname { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public AppUser.LanguageType Language { get; set; }

    public UserCreatedDomainEvent(
        Guid userId,
        string email,
        string? phoneNumber,
        bool isEmailConfirmed,
        string? name,
        string? surname,
        string? ipAddress,
        string? userAgent,
        AppUser.LanguageType language)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        PhoneNumber = phoneNumber;
        IsEmailConfirmed = isEmailConfirmed;
        Name = name;
        Surname = surname;
        Language = language;
    }
}