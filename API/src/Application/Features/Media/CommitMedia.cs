using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Application.Common.Utilities;

namespace VSky.Application.Features.Media;

/// <summary>
/// Step 2 of the two-step upload (WO-122): takes the reviewed SEO metadata, writes the parked file via the
/// file-storage service (using the SEO name), and creates the <see cref="Domain.Entities.Media"/> record.
/// The SEO file name is validated URL-safe and made unique (suffixing -2, -3, … on collision).
/// </summary>
public record CommitMediaCommand(
    string TempId,
    string SeoFileName,
    string? AltText,
    string? Title,
    string? Caption,
    string? Description) : IRequest<MediaCommitResultDto>;

public class CommitMediaCommandHandler : IRequestHandler<CommitMediaCommand, MediaCommitResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IMediaTempStore _temp;
    private readonly IFileStorage _storage;

    public CommitMediaCommandHandler(IApplicationDbContext db, IMediaTempStore temp, IFileStorage storage)
    {
        _db = db;
        _temp = temp;
        _storage = storage;
    }

    public async Task<MediaCommitResultDto> Handle(CommitMediaCommand request, CancellationToken cancellationToken)
    {
        var entry = _temp.Get(request.TempId)
            ?? throw new NotFoundException("The prepared upload could not be found — it may have expired. Please upload again.");

        var seo = (request.SeoFileName ?? string.Empty).Trim().ToLowerInvariant();
        if (!MediaHelpers.IsValidSeoFileName(seo))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.SeoFileName),
                    "The SEO file name must be lowercase letters, digits and single hyphens (e.g. blue-shirt-front)."),
            });

        seo = await MakeUniqueAsync(seo, cancellationToken);

        var fileName = seo + MediaHelpers.ExtensionForMime(entry.MimeType);
        StoredFileReference reference;
        await using (var stream = new MemoryStream(entry.Content, writable: false))
        {
            reference = await _storage.UploadAsync(new FileUploadRequest
            {
                Content = stream,
                FileName = fileName,
                ContentType = entry.MimeType,
                FolderPath = entry.MediaType.ToString().ToLowerInvariant(),
            }, cancellationToken);
        }

        var media = new Domain.Entities.Media
        {
            OriginalFileName = entry.OriginalFileName,
            SeoFileName = seo,
            AssetKey = reference.AssetKey,
            MediaType = entry.MediaType,
            MimeType = entry.MimeType,
            FileSizeBytes = entry.FileSizeBytes,
            Width = entry.Width,
            Height = entry.Height,
            AltText = request.AltText,
            Title = request.Title,
            Caption = request.Caption,
            Description = request.Description,
        };

        _db.Media.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        _temp.Remove(request.TempId);

        return new MediaCommitResultDto { MediaId = media.Id, PublicUrl = reference.PublicUrl };
    }

    /// <summary>Ensures the SEO name is unique among live media, suffixing -2, -3, … on collision.</summary>
    private async Task<string> MakeUniqueAsync(string seo, CancellationToken ct)
    {
        var existing = await _db.Media
            .AsNoTracking()
            .Where(m => m.SeoFileName == seo || m.SeoFileName.StartsWith(seo + "-"))
            .Select(m => m.SeoFileName)
            .ToListAsync(ct);

        if (!existing.Contains(seo))
            return seo;

        var taken = existing.ToHashSet();
        for (var i = 2; ; i++)
        {
            var candidate = $"{seo}-{i}";
            if (!taken.Contains(candidate))
                return candidate;
        }
    }
}
