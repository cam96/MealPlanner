using MealPlanner.Api.Household;
using MealPlanner.Contracts.Household;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps endpoints for household management, invitations, and membership.</summary>
public static class HouseholdEndpoints
{
    /// <summary>Registers the household endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/household").WithTags("Household").RequireAuthorization(AuthorizationPolicies.User);

        group.MapGet("/", GetHouseholdAsync);
        group.MapPost("/", CreateHouseholdAsync);
        group.MapPut("/", UpdateHouseholdAsync);
        group.MapPost("/leave", LeaveHouseholdAsync);
        group.MapDelete("/members/{userId:int}", RemoveMemberAsync);

        group.MapGet("/invites", GetInvitesAsync);
        group.MapPost("/invites", CreateInviteAsync);
        group.MapDelete("/invites/{id:int}", RevokeInviteAsync);

        group.MapGet("/invite/{token}", PreviewInviteAsync).AllowAnonymous().RequireAuthorization();
        group.MapPost("/join", JoinHouseholdAsync);

        return app;
    }

    private static async Task<Results<Ok<HouseholdDto>, NotFound>> GetHouseholdAsync(
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var householdId = await context.GetHouseholdIdAsync(cancellationToken);
        if (householdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households
            .AsNoTracking()
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == householdId.Value, cancellationToken);

        if (household is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToDto(household));
    }

    private static async Task<Results<Created<HouseholdDto>, Conflict<string>>> CreateHouseholdAsync(
        CreateHouseholdRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return TypedResults.Conflict("User not found.");
        }

        if (user.HouseholdId is not null)
        {
            return TypedResults.Conflict("You already belong to a household. Leave it first to create a new one.");
        }

        var household = new Domain.Entities.Household
        {
            Name = request.Name.Trim(),
            OwnerId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };

        db.Households.Add(household);
        await db.SaveChangesAsync(cancellationToken);

        // Assign the creator to the new household.
        user.HouseholdId = household.Id;
        await db.SaveChangesAsync(cancellationToken);

        // Reload with members for the response.
        await db.Entry(household).Collection(h => h.Members).LoadAsync(cancellationToken);

        return TypedResults.Created($"/api/household", ToDto(household));
    }

    private static async Task<Results<Ok<HouseholdDto>, NotFound, ForbidHttpResult>> UpdateHouseholdAsync(
        UpdateHouseholdRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user?.HouseholdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == user.HouseholdId.Value, cancellationToken);

        if (household is null)
        {
            return TypedResults.NotFound();
        }

        if (household.OwnerId != user.Id)
        {
            return TypedResults.Forbid();
        }

        household.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ToDto(household));
    }

    private static async Task<Results<NoContent, NotFound>> LeaveHouseholdAsync(
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user?.HouseholdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households.FindAsync([user.HouseholdId.Value], cancellationToken);
        if (household is null)
        {
            return TypedResults.NotFound();
        }

        // If the user is the owner, transfer ownership to the next member or delete the household.
        if (household.OwnerId == user.Id)
        {
            var nextMember = await db.AppUsers
                .Where(u => u.HouseholdId == household.Id && u.Id != user.Id)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextMember is not null)
            {
                household.OwnerId = nextMember.Id;
            }
            else
            {
                // Last member leaving — delete the household.
                db.Households.Remove(household);
            }
        }

        user.HouseholdId = null;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> RemoveMemberAsync(
        int userId,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var currentUser = await context.GetCurrentUserAsync(cancellationToken);
        if (currentUser?.HouseholdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households.FindAsync([currentUser.HouseholdId.Value], cancellationToken);
        if (household is null || household.OwnerId != currentUser.Id)
        {
            return TypedResults.Forbid();
        }

        if (userId == currentUser.Id)
        {
            return TypedResults.Forbid(); // Owner cannot remove themselves; use leave instead.
        }

        var member = await db.AppUsers
            .FirstOrDefaultAsync(u => u.Id == userId && u.HouseholdId == household.Id, cancellationToken);

        if (member is null)
        {
            return TypedResults.NotFound();
        }

        member.HouseholdId = null;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IReadOnlyList<HouseholdInviteDto>>, NotFound, ForbidHttpResult>> GetInvitesAsync(
        HouseholdContext context,
        MealPlannerDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user?.HouseholdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households.FindAsync([user.HouseholdId.Value], cancellationToken);
        if (household is null || household.OwnerId != user.Id)
        {
            return TypedResults.Forbid();
        }

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var invites = await db.HouseholdInvites
            .AsNoTracking()
            .Include(i => i.AcceptedByUser)
            .Where(i => i.HouseholdId == household.Id)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new HouseholdInviteDto(
                i.Id,
                i.Token,
                $"{baseUrl}/invite/{i.Token}",
                i.CreatedAt,
                i.ExpiresAt,
                i.Status.ToString(),
                i.AcceptedByUser != null ? i.AcceptedByUser.Name : null))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<HouseholdInviteDto>>(invites);
    }

    private static async Task<Results<Created<HouseholdInviteDto>, NotFound, ForbidHttpResult>> CreateInviteAsync(
        HouseholdContext context,
        MealPlannerDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user?.HouseholdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households.FindAsync([user.HouseholdId.Value], cancellationToken);
        if (household is null || household.OwnerId != user.Id)
        {
            return TypedResults.Forbid();
        }

        var invite = new HouseholdInvite
        {
            HouseholdId = household.Id,
            Token = Guid.NewGuid().ToString("N"),
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = InviteStatus.Pending,
        };

        db.HouseholdInvites.Add(invite);
        await db.SaveChangesAsync(cancellationToken);

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var dto = new HouseholdInviteDto(
            invite.Id,
            invite.Token,
            $"{baseUrl}/invite/{invite.Token}",
            invite.CreatedAt,
            invite.ExpiresAt,
            invite.Status.ToString(),
            null);

        return TypedResults.Created($"/api/household/invites", dto);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> RevokeInviteAsync(
        int id,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user?.HouseholdId is null)
        {
            return TypedResults.NotFound();
        }

        var household = await db.Households.FindAsync([user.HouseholdId.Value], cancellationToken);
        if (household is null || household.OwnerId != user.Id)
        {
            return TypedResults.Forbid();
        }

        var invite = await db.HouseholdInvites
            .FirstOrDefaultAsync(i => i.Id == id && i.HouseholdId == household.Id, cancellationToken);

        if (invite is null)
        {
            return TypedResults.NotFound();
        }

        invite.Status = InviteStatus.Revoked;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<InvitePreviewDto>, NotFound>> PreviewInviteAsync(
        string token,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var invite = await db.HouseholdInvites
            .AsNoTracking()
            .Include(i => i.Household)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invite?.Household is null || invite.CreatedByUser is null)
        {
            return TypedResults.NotFound();
        }

        var preview = new InvitePreviewDto(
            invite.Household.Name,
            invite.CreatedByUser.Name,
            invite.ExpiresAt,
            invite.ExpiresAt < DateTime.UtcNow,
            invite.Status == InviteStatus.Accepted);

        return TypedResults.Ok(preview);
    }

    private static async Task<Results<Ok<HouseholdDto>, NotFound, Conflict<string>>> JoinHouseholdAsync(
        AcceptInviteRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await context.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (user.HouseholdId is not null)
        {
            return TypedResults.Conflict("You already belong to a household. Leave it first to join another.");
        }

        var invite = await db.HouseholdInvites
            .Include(i => i.Household!)
                .ThenInclude(h => h.Members)
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        if (invite is null)
        {
            return TypedResults.NotFound();
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return TypedResults.Conflict("This invitation has already been used or revoked.");
        }

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            return TypedResults.Conflict("This invitation has expired.");
        }

        // Accept the invite and join the household.
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedByUserId = user.Id;
        user.HouseholdId = invite.HouseholdId;
        await db.SaveChangesAsync(cancellationToken);

        // Reload household with updated members.
        var household = await db.Households
            .AsNoTracking()
            .Include(h => h.Members)
            .FirstAsync(h => h.Id == invite.HouseholdId, cancellationToken);

        return TypedResults.Ok(ToDto(household));
    }

    private static HouseholdDto ToDto(Domain.Entities.Household household)
    {
        var ownerName = household.Members.FirstOrDefault(m => m.Id == household.OwnerId)?.Name ?? "";
        var members = household.Members
            .Select(m => new HouseholdMemberDto(m.Id, m.Name, m.Email, m.Id == household.OwnerId))
            .ToList();

        return new HouseholdDto(household.Id, household.Name, household.OwnerId, ownerName, members);
    }
}
