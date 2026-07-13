using FluentValidation;
using MediatR;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.IntegrationCredentials;

// ============================ Stripe =======================================

public class StripeCredentialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool IsProduction { get; set; }
    public string? PublishableKey { get; set; }
    public string? SecretKey { get; set; }
    public string? ReturnUrl { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public static StripeCredentialDto From(StripeCredential e) => new()
    {
        Id = e.Id, Name = e.Name, Active = e.Active, IsProduction = e.IsProduction,
        PublishableKey = e.PublishableKey, SecretKey = e.SecretKey, ReturnUrl = e.ReturnUrl,
        CreatedOnUtc = e.CreatedOnUtc, UpdatedOnUtc = e.UpdatedOnUtc,
    };
}

public record ListStripeCredentialsQuery : IRequest<IReadOnlyList<IntegrationCredentialListItemDto>>;

public class ListStripeCredentialsQueryHandler
    : IRequestHandler<ListStripeCredentialsQuery, IReadOnlyList<IntegrationCredentialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public ListStripeCredentialsQueryHandler(IApplicationDbContext db) => _db = db;
    public Task<IReadOnlyList<IntegrationCredentialListItemDto>> Handle(ListStripeCredentialsQuery request, CancellationToken ct)
        => IntegrationCredentialSupport.ListAsync(_db.StripeCredentials, ct);
}

public record GetStripeCredentialQuery(Guid Id) : IRequest<StripeCredentialDto>;

public class GetStripeCredentialQueryHandler : IRequestHandler<GetStripeCredentialQuery, StripeCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public GetStripeCredentialQueryHandler(IApplicationDbContext db) => _db = db;
    public async Task<StripeCredentialDto> Handle(GetStripeCredentialQuery request, CancellationToken ct)
        => StripeCredentialDto.From(await IntegrationCredentialSupport.GetAsync(_db.StripeCredentials, request.Id, ct));
}

public record SaveStripeCredentialCommand(
    Guid? Id, string Name, bool Active, bool IsProduction,
    string? PublishableKey, string? SecretKey, string? ReturnUrl) : IRequest<StripeCredentialDto>;

public class SaveStripeCredentialCommandValidator : AbstractValidator<SaveStripeCredentialCommand>
{
    public SaveStripeCredentialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SecretKey).NotEmpty();
    }
}

public class SaveStripeCredentialCommandHandler : IRequestHandler<SaveStripeCredentialCommand, StripeCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public SaveStripeCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public async Task<StripeCredentialDto> Handle(SaveStripeCredentialCommand request, CancellationToken ct)
    {
        var e = await IntegrationCredentialSupport.UpsertAsync(_db.StripeCredentials, request.Id, request.Name, request.Active, request.IsProduction, ct);
        e.PublishableKey = IntegrationCredentialSupport.Norm(request.PublishableKey);
        e.SecretKey = IntegrationCredentialSupport.Norm(request.SecretKey);
        e.ReturnUrl = IntegrationCredentialSupport.Norm(request.ReturnUrl);
        await _db.SaveChangesAsync(ct);
        return StripeCredentialDto.From(e);
    }
}

public record DeleteStripeCredentialCommand(Guid Id) : IRequest;

public class DeleteStripeCredentialCommandHandler : IRequestHandler<DeleteStripeCredentialCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteStripeCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public Task Handle(DeleteStripeCredentialCommand request, CancellationToken ct)
        => IntegrationCredentialSupport.DeleteAsync(_db.StripeCredentials, _db, request.Id, ct);
}

// ============================ PayPal =======================================

public class PayPalCredentialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool IsProduction { get; set; }
    public string? ClientId { get; set; }
    public string? SecretKey { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public static PayPalCredentialDto From(PayPalCredential e) => new()
    {
        Id = e.Id, Name = e.Name, Active = e.Active, IsProduction = e.IsProduction,
        ClientId = e.ClientId, SecretKey = e.SecretKey,
        CreatedOnUtc = e.CreatedOnUtc, UpdatedOnUtc = e.UpdatedOnUtc,
    };
}

public record ListPayPalCredentialsQuery : IRequest<IReadOnlyList<IntegrationCredentialListItemDto>>;

public class ListPayPalCredentialsQueryHandler
    : IRequestHandler<ListPayPalCredentialsQuery, IReadOnlyList<IntegrationCredentialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public ListPayPalCredentialsQueryHandler(IApplicationDbContext db) => _db = db;
    public Task<IReadOnlyList<IntegrationCredentialListItemDto>> Handle(ListPayPalCredentialsQuery request, CancellationToken ct)
        => IntegrationCredentialSupport.ListAsync(_db.PayPalCredentials, ct);
}

public record GetPayPalCredentialQuery(Guid Id) : IRequest<PayPalCredentialDto>;

public class GetPayPalCredentialQueryHandler : IRequestHandler<GetPayPalCredentialQuery, PayPalCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public GetPayPalCredentialQueryHandler(IApplicationDbContext db) => _db = db;
    public async Task<PayPalCredentialDto> Handle(GetPayPalCredentialQuery request, CancellationToken ct)
        => PayPalCredentialDto.From(await IntegrationCredentialSupport.GetAsync(_db.PayPalCredentials, request.Id, ct));
}

public record SavePayPalCredentialCommand(
    Guid? Id, string Name, bool Active, bool IsProduction,
    string? ClientId, string? SecretKey) : IRequest<PayPalCredentialDto>;

public class SavePayPalCredentialCommandValidator : AbstractValidator<SavePayPalCredentialCommand>
{
    public SavePayPalCredentialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.SecretKey).NotEmpty();
    }
}

public class SavePayPalCredentialCommandHandler : IRequestHandler<SavePayPalCredentialCommand, PayPalCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public SavePayPalCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public async Task<PayPalCredentialDto> Handle(SavePayPalCredentialCommand request, CancellationToken ct)
    {
        var e = await IntegrationCredentialSupport.UpsertAsync(_db.PayPalCredentials, request.Id, request.Name, request.Active, request.IsProduction, ct);
        e.ClientId = IntegrationCredentialSupport.Norm(request.ClientId);
        e.SecretKey = IntegrationCredentialSupport.Norm(request.SecretKey);
        await _db.SaveChangesAsync(ct);
        return PayPalCredentialDto.From(e);
    }
}

public record DeletePayPalCredentialCommand(Guid Id) : IRequest;

public class DeletePayPalCredentialCommandHandler : IRequestHandler<DeletePayPalCredentialCommand>
{
    private readonly IApplicationDbContext _db;
    public DeletePayPalCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public Task Handle(DeletePayPalCredentialCommand request, CancellationToken ct)
        => IntegrationCredentialSupport.DeleteAsync(_db.PayPalCredentials, _db, request.Id, ct);
}

// ============================ Razorpay =====================================

public class RazorpayCredentialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool IsProduction { get; set; }
    public string? KeyId { get; set; }
    public string? KeySecret { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public static RazorpayCredentialDto From(RazorpayCredential e) => new()
    {
        Id = e.Id, Name = e.Name, Active = e.Active, IsProduction = e.IsProduction,
        KeyId = e.KeyId, KeySecret = e.KeySecret,
        CreatedOnUtc = e.CreatedOnUtc, UpdatedOnUtc = e.UpdatedOnUtc,
    };
}

public record ListRazorpayCredentialsQuery : IRequest<IReadOnlyList<IntegrationCredentialListItemDto>>;

public class ListRazorpayCredentialsQueryHandler
    : IRequestHandler<ListRazorpayCredentialsQuery, IReadOnlyList<IntegrationCredentialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public ListRazorpayCredentialsQueryHandler(IApplicationDbContext db) => _db = db;
    public Task<IReadOnlyList<IntegrationCredentialListItemDto>> Handle(ListRazorpayCredentialsQuery request, CancellationToken ct)
        => IntegrationCredentialSupport.ListAsync(_db.RazorpayCredentials, ct);
}

public record GetRazorpayCredentialQuery(Guid Id) : IRequest<RazorpayCredentialDto>;

public class GetRazorpayCredentialQueryHandler : IRequestHandler<GetRazorpayCredentialQuery, RazorpayCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public GetRazorpayCredentialQueryHandler(IApplicationDbContext db) => _db = db;
    public async Task<RazorpayCredentialDto> Handle(GetRazorpayCredentialQuery request, CancellationToken ct)
        => RazorpayCredentialDto.From(await IntegrationCredentialSupport.GetAsync(_db.RazorpayCredentials, request.Id, ct));
}

public record SaveRazorpayCredentialCommand(
    Guid? Id, string Name, bool Active, bool IsProduction,
    string? KeyId, string? KeySecret) : IRequest<RazorpayCredentialDto>;

public class SaveRazorpayCredentialCommandValidator : AbstractValidator<SaveRazorpayCredentialCommand>
{
    public SaveRazorpayCredentialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.KeyId).NotEmpty();
        RuleFor(x => x.KeySecret).NotEmpty();
    }
}

public class SaveRazorpayCredentialCommandHandler : IRequestHandler<SaveRazorpayCredentialCommand, RazorpayCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public SaveRazorpayCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public async Task<RazorpayCredentialDto> Handle(SaveRazorpayCredentialCommand request, CancellationToken ct)
    {
        var e = await IntegrationCredentialSupport.UpsertAsync(_db.RazorpayCredentials, request.Id, request.Name, request.Active, request.IsProduction, ct);
        e.KeyId = IntegrationCredentialSupport.Norm(request.KeyId);
        e.KeySecret = IntegrationCredentialSupport.Norm(request.KeySecret);
        await _db.SaveChangesAsync(ct);
        return RazorpayCredentialDto.From(e);
    }
}

public record DeleteRazorpayCredentialCommand(Guid Id) : IRequest;

public class DeleteRazorpayCredentialCommandHandler : IRequestHandler<DeleteRazorpayCredentialCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteRazorpayCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public Task Handle(DeleteRazorpayCredentialCommand request, CancellationToken ct)
        => IntegrationCredentialSupport.DeleteAsync(_db.RazorpayCredentials, _db, request.Id, ct);
}

// ============================ Square =======================================

public class SquareCredentialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool IsProduction { get; set; }
    public string? ApplicationId { get; set; }
    public string? AccessToken { get; set; }
    public string? ApplicationSecret { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public static SquareCredentialDto From(SquareCredential e) => new()
    {
        Id = e.Id, Name = e.Name, Active = e.Active, IsProduction = e.IsProduction,
        ApplicationId = e.ApplicationId, AccessToken = e.AccessToken, ApplicationSecret = e.ApplicationSecret,
        CreatedOnUtc = e.CreatedOnUtc, UpdatedOnUtc = e.UpdatedOnUtc,
    };
}

public record ListSquareCredentialsQuery : IRequest<IReadOnlyList<IntegrationCredentialListItemDto>>;

public class ListSquareCredentialsQueryHandler
    : IRequestHandler<ListSquareCredentialsQuery, IReadOnlyList<IntegrationCredentialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public ListSquareCredentialsQueryHandler(IApplicationDbContext db) => _db = db;
    public Task<IReadOnlyList<IntegrationCredentialListItemDto>> Handle(ListSquareCredentialsQuery request, CancellationToken ct)
        => IntegrationCredentialSupport.ListAsync(_db.SquareCredentials, ct);
}

public record GetSquareCredentialQuery(Guid Id) : IRequest<SquareCredentialDto>;

public class GetSquareCredentialQueryHandler : IRequestHandler<GetSquareCredentialQuery, SquareCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public GetSquareCredentialQueryHandler(IApplicationDbContext db) => _db = db;
    public async Task<SquareCredentialDto> Handle(GetSquareCredentialQuery request, CancellationToken ct)
        => SquareCredentialDto.From(await IntegrationCredentialSupport.GetAsync(_db.SquareCredentials, request.Id, ct));
}

public record SaveSquareCredentialCommand(
    Guid? Id, string Name, bool Active, bool IsProduction,
    string? ApplicationId, string? AccessToken, string? ApplicationSecret) : IRequest<SquareCredentialDto>;

public class SaveSquareCredentialCommandValidator : AbstractValidator<SaveSquareCredentialCommand>
{
    public SaveSquareCredentialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccessToken).NotEmpty();
    }
}

public class SaveSquareCredentialCommandHandler : IRequestHandler<SaveSquareCredentialCommand, SquareCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public SaveSquareCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public async Task<SquareCredentialDto> Handle(SaveSquareCredentialCommand request, CancellationToken ct)
    {
        var e = await IntegrationCredentialSupport.UpsertAsync(_db.SquareCredentials, request.Id, request.Name, request.Active, request.IsProduction, ct);
        e.ApplicationId = IntegrationCredentialSupport.Norm(request.ApplicationId);
        e.AccessToken = IntegrationCredentialSupport.Norm(request.AccessToken);
        e.ApplicationSecret = IntegrationCredentialSupport.Norm(request.ApplicationSecret);
        await _db.SaveChangesAsync(ct);
        return SquareCredentialDto.From(e);
    }
}

public record DeleteSquareCredentialCommand(Guid Id) : IRequest;

public class DeleteSquareCredentialCommandHandler : IRequestHandler<DeleteSquareCredentialCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteSquareCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public Task Handle(DeleteSquareCredentialCommand request, CancellationToken ct)
        => IntegrationCredentialSupport.DeleteAsync(_db.SquareCredentials, _db, request.Id, ct);
}

// ============================ Authorize.Net ================================

public class AuthorizeNetCredentialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool IsProduction { get; set; }
    public string? ApplicationLoginId { get; set; }
    public string? TransactionKey { get; set; }
    public string? SignatureKey { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public static AuthorizeNetCredentialDto From(AuthorizeNetCredential e) => new()
    {
        Id = e.Id, Name = e.Name, Active = e.Active, IsProduction = e.IsProduction,
        ApplicationLoginId = e.ApplicationLoginId, TransactionKey = e.TransactionKey, SignatureKey = e.SignatureKey,
        CreatedOnUtc = e.CreatedOnUtc, UpdatedOnUtc = e.UpdatedOnUtc,
    };
}

public record ListAuthorizeNetCredentialsQuery : IRequest<IReadOnlyList<IntegrationCredentialListItemDto>>;

public class ListAuthorizeNetCredentialsQueryHandler
    : IRequestHandler<ListAuthorizeNetCredentialsQuery, IReadOnlyList<IntegrationCredentialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public ListAuthorizeNetCredentialsQueryHandler(IApplicationDbContext db) => _db = db;
    public Task<IReadOnlyList<IntegrationCredentialListItemDto>> Handle(ListAuthorizeNetCredentialsQuery request, CancellationToken ct)
        => IntegrationCredentialSupport.ListAsync(_db.AuthorizeNetCredentials, ct);
}

public record GetAuthorizeNetCredentialQuery(Guid Id) : IRequest<AuthorizeNetCredentialDto>;

public class GetAuthorizeNetCredentialQueryHandler : IRequestHandler<GetAuthorizeNetCredentialQuery, AuthorizeNetCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public GetAuthorizeNetCredentialQueryHandler(IApplicationDbContext db) => _db = db;
    public async Task<AuthorizeNetCredentialDto> Handle(GetAuthorizeNetCredentialQuery request, CancellationToken ct)
        => AuthorizeNetCredentialDto.From(await IntegrationCredentialSupport.GetAsync(_db.AuthorizeNetCredentials, request.Id, ct));
}

public record SaveAuthorizeNetCredentialCommand(
    Guid? Id, string Name, bool Active, bool IsProduction,
    string? ApplicationLoginId, string? TransactionKey, string? SignatureKey) : IRequest<AuthorizeNetCredentialDto>;

public class SaveAuthorizeNetCredentialCommandValidator : AbstractValidator<SaveAuthorizeNetCredentialCommand>
{
    public SaveAuthorizeNetCredentialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ApplicationLoginId).NotEmpty();
        RuleFor(x => x.TransactionKey).NotEmpty();
    }
}

public class SaveAuthorizeNetCredentialCommandHandler : IRequestHandler<SaveAuthorizeNetCredentialCommand, AuthorizeNetCredentialDto>
{
    private readonly IApplicationDbContext _db;
    public SaveAuthorizeNetCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public async Task<AuthorizeNetCredentialDto> Handle(SaveAuthorizeNetCredentialCommand request, CancellationToken ct)
    {
        var e = await IntegrationCredentialSupport.UpsertAsync(_db.AuthorizeNetCredentials, request.Id, request.Name, request.Active, request.IsProduction, ct);
        e.ApplicationLoginId = IntegrationCredentialSupport.Norm(request.ApplicationLoginId);
        e.TransactionKey = IntegrationCredentialSupport.Norm(request.TransactionKey);
        e.SignatureKey = IntegrationCredentialSupport.Norm(request.SignatureKey);
        await _db.SaveChangesAsync(ct);
        return AuthorizeNetCredentialDto.From(e);
    }
}

public record DeleteAuthorizeNetCredentialCommand(Guid Id) : IRequest;

public class DeleteAuthorizeNetCredentialCommandHandler : IRequestHandler<DeleteAuthorizeNetCredentialCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteAuthorizeNetCredentialCommandHandler(IApplicationDbContext db) => _db = db;
    public Task Handle(DeleteAuthorizeNetCredentialCommand request, CancellationToken ct)
        => IntegrationCredentialSupport.DeleteAsync(_db.AuthorizeNetCredentials, _db, request.Id, ct);
}
