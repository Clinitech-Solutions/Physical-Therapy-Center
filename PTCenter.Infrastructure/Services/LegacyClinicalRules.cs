using Microsoft.EntityFrameworkCore;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

// Legacy drafts share the assignment and serialization boundary of the clinical workspace.
// Signed history has no approved amendment workflow; reassessment records a new clinical decision.
internal sealed class LegacyClinicalRules(ApplicationDbContext db)
{
    internal static Error Denied => new("Clinical.AccessDenied", "Only the assigned Senior or an authorized supervisor may change this clinical draft.", 403);
    internal static Error Signed => new("Clinical.SignedRecord", "Signed or clinically used records cannot be overwritten or deleted. Use the reassessment workflow for a new clinical decision; signed amendments require an approved auditable policy.", 409);
    internal static bool Owns(string owner, string actor, bool supervisor) => !string.IsNullOrWhiteSpace(actor) && (supervisor || owner == actor);

    internal async Task<Error?> AssessmentAsync(Assessment assessment, string actor, bool supervisor, bool mutable)
    {
        if (!Owns(assessment.SeniorId, actor, supervisor)) return Denied;
        if (!supervisor && assessment.AppointmentId is int id && !await db.Appointments.AnyAsync(a=>a.Id==id && a.SeniorId==actor)) return Denied;
        if (mutable && (assessment.CompletedAt != null || await ReliedUponAsync(assessment.Id))) return Signed;
        return null;
    }

    internal async Task<Error?> PlanAsync(int planId, string actor, bool supervisor, bool mutable)
    {
        var plan = await db.TreatmentPlans.Include(p=>p.Assessment).SingleOrDefaultAsync(p=>p.Id==planId);
        if (plan == null) return TreatmentPlanErrors.TreatmentPlanNotFound;
        return await AssessmentAsync(plan.Assessment, actor, supervisor, mutable);
    }

    private async Task<bool> ReliedUponAsync(int assessmentId) =>
        await db.TreatmentCourseAgreements.AnyAsync(c=>db.TreatmentPlans.Any(p=>p.Id==c.TreatmentPlanId && p.AssessmentId==assessmentId)) ||
        await db.Appointments.AnyAsync(a=>a.TreatmentPlanSession!=null && a.TreatmentPlanSession.TreatmentPlan.AssessmentId==assessmentId) ||
        await db.TreatmentPlanSessions.AnyAsync(s=>s.TreatmentPlan.AssessmentId==assessmentId && s.Status!=SessionStatus.Pending);

    internal async Task<bool> EligibleAsync(string userId, int patientId, string role)
    {
        var eligible = await (from user in db.Users
         join membership in db.UserRoles on user.Id equals membership.UserId
         join userRole in db.Roles on membership.RoleId equals userRole.Id
         where user.Id==userId && userRole.Name==role
             && db.Patients.Any(p=>p.Id==patientId && p.ApplicationUser.Gender==user.Gender)
         select user).SingleOrDefaultAsync();
        return eligible != null && (eligible.LockoutEnd == null || eligible.LockoutEnd <= DateTimeOffset.UtcNow);
    }
}
