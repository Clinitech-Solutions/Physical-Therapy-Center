using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Doctor;
using PTCenter.Infrastructure.Persistence;
using Xunit;

namespace PTCenter.Tests;

public class ReassessmentIntegrationTests
{
    [Fact]
    public async Task Senior_detail_returns_saved_plan_and_final_clinical_history_from_the_requested_course()
    {
        using var host = new BookingTestHost();
        await DoctorWorkspaceFixture.SeedAsync(host);
        var id = await DoctorWorkspaceFixture.BookAsync(host);
        await DoctorWorkspaceFixture.ArriveAsync(host, id);
        using var doctor = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await DoctorWorkspaceFixture.LoginAsync(doctor, "access.doctor");
        var start = await doctor.PostAsJsonAsync($"/api/Doctor/appointments/{id}/start", new { });
        var active = (await start.Content.ReadFromJsonAsync<DoctorPatientWorkspace>())!;
        var notes = new NotesMutation(active.Notes.Version, "Saved progress", "Saved symptom change", "Saved response", "Saved treatment");
        Assert.Equal(HttpStatusCode.OK, (await doctor.PostAsJsonAsync($"/api/Doctor/appointments/{id}/end", notes)).StatusCode);
        var response = await doctor.PostAsJsonAsync($"/api/ReassessmentRequests/appointments/{id}", new RequestReassessment("history-review", "PlanReview", "Review the saved clinical history", "Continue"));
        var request = (await response.Content.ReadFromJsonAsync<ReassessmentView>())!;
        using var senior = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await DoctorWorkspaceFixture.LoginAsync(senior, "clinical.senior");
        var detail = (await senior.GetFromJsonAsync<ReassessmentView>($"/api/ReassessmentRequests/{request.Id}"))!;
        Assert.Equal(active.Plan.Id, detail.TreatmentPlanId);
        Assert.Equal(active.Plan.Diagnosis, detail.Diagnosis);
        var history = Assert.Single(detail.PreviousSessions);
        Assert.Equal(notes.ProgressNotes, history.ProgressNotes);
        Assert.Equal(notes.ConditionChanges, history.ConditionChanges);
        Assert.Equal(notes.ResponseToTreatment, history.ResponseToTreatment);
        Assert.Equal(notes.TreatmentPerformed, history.TreatmentPerformed);
        Assert.NotNull(history.TreatmentStartedAt);
        Assert.NotNull(history.TreatmentEndedAt);
        Assert.NotNull(history.RecordedAt);
    }

    // These are real controller/Identity/EF integration tests over disposable SQLite, not SQL Server.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Free_review_roundtrip_preserves_clinical_and_financial_state_and_rejects_conflicting_retries(int patientId)
    {
        using var host = new BookingTestHost();
        await DoctorWorkspaceFixture.SeedAsync(host);
        var appointmentId = await DoctorWorkspaceFixture.BookAsync(host, patient: patientId, deposit: 0);
        using var doctor = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        using var senior = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        using var other = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await DoctorWorkspaceFixture.LoginAsync(doctor, "access.doctor");
        await DoctorWorkspaceFixture.LoginAsync(senior, "clinical.senior");
        var before = await Snapshot(host);
        var intent = new RequestReassessment("integration-review", "ProgressConcern", "Please review during this visit", "Pause");
        var created = await doctor.PostAsJsonAsync($"/api/ReassessmentRequests/appointments/{appointmentId}", intent);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var request = (await created.Content.ReadFromJsonAsync<ReassessmentView>())!;
        Assert.Equal(patientId, request.PatientId);
        Assert.Equal(appointmentId, request.RelatedAppointmentId);
        var duplicate = await doctor.PostAsJsonAsync($"/api/ReassessmentRequests/appointments/{appointmentId}", intent);
        Assert.Equal(request.Id, (await duplicate.Content.ReadFromJsonAsync<ReassessmentView>())!.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await doctor.PostAsJsonAsync($"/api/ReassessmentRequests/appointments/{appointmentId}", intent with { OperationKey = "second-tab" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await doctor.PostAsJsonAsync($"/api/ReassessmentRequests/appointments/{appointmentId}", intent with { Notes = "Changed retry" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doctor.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/start", new StartReassessment(1))).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(other, "clinical.otherdoctor");
        Assert.Empty((await other.GetFromJsonAsync<List<ReassessmentView>>("/api/ReassessmentRequests"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/ReassessmentRequests/{request.Id}")).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(other, "clinical.femaleSenior");
        Assert.Empty((await other.GetFromJsonAsync<List<ReassessmentView>>("/api/ReassessmentRequests"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/start", new StartReassessment(1))).StatusCode);
        var pending = Assert.Single((await senior.GetFromJsonAsync<List<ReassessmentView>>("/api/ReassessmentRequests"))!);
        Assert.Equal(request.TreatmentPlanId, pending.TreatmentPlanId);
        var start = await senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/start", new StartReassessment(request.Version));
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var reviewing = (await start.Content.ReadFromJsonAsync<ReassessmentView>())!;
        var completion = new CompleteReassessment(reviewing.Version, "Saved clinical outcome: continue the current plan", "Continue", "", "", 0, 0, "", null);
        Assert.Equal(HttpStatusCode.BadRequest, (await senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/complete", completion with { ProgressNotes = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/complete", completion with { ExpectedVersion = 1 })).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(other, "clinical.otherSenior");
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/complete", completion)).StatusCode);
        var completions = await Task.WhenAll(
            senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/complete", completion),
            senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/complete", completion with { ProgressNotes = "Conflicting other-tab outcome" }));
        Assert.Single(completions, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(completions, r => r.StatusCode == HttpStatusCode.Conflict);
        var finished = (await completions.Single(r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<ReassessmentView>())!;
        var winningIntent = completion with { ProgressNotes = finished.ReviewNotes! };
        var repeated = await senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/complete", winningIntent);
        Assert.Equal(finished.CompletedAssessmentId, (await repeated.Content.ReadFromJsonAsync<ReassessmentView>())!.CompletedAssessmentId);
        Assert.Equal(HttpStatusCode.Conflict, (await senior.PostAsJsonAsync($"/api/ReassessmentRequests/{request.Id}/cancel", new CancelReassessment(finished.Version, "Too late"))).StatusCode);
        var doctorResult = (await doctor.GetFromJsonAsync<DoctorPatientWorkspace>($"/api/Doctor/appointments/{appointmentId}"))!.Reassessments.Single();
        Assert.Equal("Completed", doctorResult.State);
        Assert.Equal(finished.ReviewNotes, doctorResult.ReviewNotes);
        Assert.Equal("Local senior", doctorResult.SeniorName);
        Assert.NotNull(doctorResult.ResolvedAt);
        Assert.Equal(doctorResult.SeniorName, doctorResult.ResolvedBy);
        Assert.Equal("Completed", (await senior.GetFromJsonAsync<ReassessmentView>($"/api/ReassessmentRequests/{request.Id}"))!.State);
        Assert.Equal(before, await Snapshot(host));
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.ReassessmentRequests.ToListAsync());
        Assert.Single(await db.ReAssessments.ToListAsync());
    }

    private static async Task<string> Snapshot(BookingTestHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return JsonSerializer.Serialize(new {
            Appointments = await db.Appointments.AsNoTracking().OrderBy(a => a.Id).Select(a => new { a.Id, a.Status, a.StartTime, a.EndTime, a.ActualCheckIn, a.ActualCheckOut, a.TreatmentStartedAt, a.TreatmentEndedAt }).ToListAsync(),
            Plans = await db.TreatmentPlans.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.AssessmentId, p.NumberOfSessions, p.CompletedSessions, p.Status }).ToListAsync(),
            Invoices = await db.Invoices.AsNoTracking().OrderBy(i => i.Id).Select(i => new { i.Id, i.TotalAmount, i.PatientAmount, i.InsuranceAmount, i.PaidAmount }).ToListAsync(),
            Payments = await db.Payments.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.Amount, p.Status }).ToListAsync(),
            Approvals = await db.Patients.AsNoTracking().OrderBy(i => i.Id).Select(p => new { p.Id, p.PaymentType, Status = p.InsuranceDetails == null ? null : p.InsuranceDetails.Status, Copay = p.InsuranceDetails == null ? null : p.InsuranceDetails.CopayPercentage, Limit = p.InsuranceDetails == null ? null : p.InsuranceDetails.ApprovedSessions }).ToListAsync(),
            Agreements = await db.TreatmentCourseAgreements.AsNoTracking().OrderBy(i => i.Id).Select(i => new { i.Id, i.TotalAmount, i.PatientAmount, i.InsuranceAmount }).ToListAsync(),
            Claims = await db.InsuranceClaims.CountAsync()
        });
    }
}
