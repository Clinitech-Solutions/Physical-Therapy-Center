using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Application.Contracts.Doctor;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public class AbsenceRecoveryHttpTests
{
    private static readonly DateOnly Day = new(2030, 2, 4);
    private static readonly DateTimeOffset Start = new(2030, 2, 4, 10, 0, 0, TimeSpan.Zero);
    private static HttpClient Client(BookingTestHost host) => host.CreateClient(new() { BaseAddress = new("https://localhost") });
    private static async Task<string> User(BookingTestHost host, string name) { using var scope = host.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.Where(u => u.UserName == name).Select(u => u.Id).SingleAsync(); }
    private static AbsenceRequest Absence(string clinician, bool full = true, TimeOnly? from = null, TimeOnly? to = null) => new(Guid.NewGuid().ToString(), clinician, Day, full, from, to, "Synthetic clinician absence");
    private static async Task<AbsenceView> Save(HttpClient client, AbsenceRequest request) { var r = await client.PostAsJsonAsync("/api/Absences", request); Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync()); return (await r.Content.ReadFromJsonAsync<AbsenceView>())!; }
    private static async Task<RecoveryOptions> Options(HttpClient c, int id) => (await c.GetFromJsonAsync<RecoveryOptions>($"/api/Absences/appointments/{id}/options"))!;
    private static RecoveryRequest Resolve(RecoveryOptions o, RecoveryCandidate c, string action = "Replace") => new(Guid.NewGuid().ToString(), o.Appointment.Version, o.Appointment.AbsenceIds[0], c.ClinicianId, new DateTimeOffset(DateTime.SpecifyKind(c.Start, DateTimeKind.Utc)), c.RoomId, action, "Patient confirmed the replacement");
    private static async Task<int> Assessment(BookingTestHost host, string senior, DateTimeOffset? start = null, int room = 2)
    {
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = Guid.NewGuid().ToString("N"); var p = new Patient { ApplicationUser = new() { Id = key, UserName = key, FullName = "Synthetic Initial Assessment", Gender = Gender.Male }, PaymentType = "Cash" }; db.Patients.Add(p); await db.SaveChangesAsync();
        var receipt = await scope.ServiceProvider.GetRequiredService<BookingService>().CreateAsync(new() { IdempotencyKey = Guid.NewGuid().ToString(), PatientId = p.Id, ClinicianId = senior, RoomId = room, StartsAt = [start ?? Start], AssessmentCharge = "Free" }); return receipt.Appointments[0].Id;
    }
    private static async Task<string> Financials(BookingTestHost host)
    {
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return JsonSerializer.Serialize(new { Invoices = await db.Invoices.OrderBy(x => x.Id).Select(x => new { x.Id, x.AppointmentId, x.TreatmentPlanSessionId, x.CourseAgreementId, x.TotalAmount, x.PatientAmount, x.InsuranceAmount, x.PaidAmount }).ToListAsync(), Payments = await db.Payments.OrderBy(x => x.Id).Select(x => new { x.Id, x.InvoiceId, x.Amount, x.PaymentMethod, x.Status }).ToListAsync(), Plans = await db.TreatmentPlans.OrderBy(x => x.Id).Select(x => new { x.Id, x.DoctorId, x.NumberOfSessions, x.AssessmentId }).ToListAsync() });
    }
    [Fact]
    public async Task Full_day_and_partial_absences_count_partial_overlap_deduplicate_exclude_started_and_preserve_schedule()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); var doctor = await User(host, "access.doctor");
        // 12:30 Cairo partially overlaps the saved 12:00-13:00 booking.
        var partial = await Save(c, Absence(doctor, false, new(12, 30), new(13, 30))); Assert.Single(partial.Affected); Assert.Equal(id, partial.Affected[0].AppointmentId);
        await Save(c, Absence(doctor)); Assert.Single((await c.GetFromJsonAsync<List<RecoveryBooking>>("/api/Absences/affected"))!);
        var blockedBooking = new BookingRequest { IdempotencyKey = Guid.NewGuid().ToString(), PatientId = 2, TreatmentPlanId = 2, ClinicianId = doctor, RoomId = 3, StartsAt = [Start], UnitPrice = 100 };
        var p = await c.PostAsJsonAsync("/api/Bookings/preview", blockedBooking); var preview = (await p.Content.ReadFromJsonAsync<BookingPreview>())!; Assert.False(preview.CanSave); Assert.Contains("ClinicianAbsent", preview.Appointments[0].Conflicts);
        var finances = await Financials(host);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/Bookings", blockedBooking)).StatusCode);
        Assert.Equal(finances, await Financials(host));
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Bookings/appointments/{id}/status", 4)).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(c, "access.doctor"); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/Doctor/appointments/{id}/start", new { })).StatusCode); await ReceptionAccessFixture.LoginAsync(c);
        var senior = await User(host, "clinical.senior"); var assessment = await Assessment(host, senior, Start.AddHours(2)); var seniorAbsence = await Save(c, Absence(senior)); Assert.Single(seniorAbsence.Affected); Assert.Equal(assessment, seniorAbsence.Affected[0].AppointmentId);
        using (var scope = host.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var a = await db.Appointments.SingleAsync(x => x.Id == id); Assert.Equal(Start.UtcDateTime, a.StartTime); a.TreatmentStartedAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
        Assert.DoesNotContain((await c.GetFromJsonAsync<List<RecoveryBooking>>("/api/Absences/affected"))!, x => x.AppointmentId == id);
    }
    [Fact]
    public async Task Assessment_requires_Senior_and_treatment_can_be_covered_by_Senior_with_real_clinical_completion_once()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var senior = await User(host, "clinical.senior"); var otherSenior = await User(host, "clinical.otherSenior"); var otherDoctor = await User(host, "clinical.otherdoctor");
        var assessment = await Assessment(host, senior); await Save(c, Absence(senior)); var assessmentOptions = await Options(c, assessment); Assert.All(assessmentOptions.Matching, x => Assert.Equal("Senior", x.Role));
        var wrong = Resolve(assessmentOptions, assessmentOptions.Matching.First()) with { ClinicianId = otherDoctor }; Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/Absences/appointments/{assessment}/resolve", wrong)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/appointments/{assessment}/resolve", Resolve(assessmentOptions, assessmentOptions.Matching.First(x => x.ClinicianId == otherSenior)))).StatusCode);
        var treatment = await DoctorWorkspaceFixture.BookAsync(host, room: 1, day: 1); var doctor = await User(host, "access.doctor"); await Save(c, Absence(doctor) with { Date = Day.AddDays(1) });
        var options = await Options(c, treatment); var cover = options.Matching.First(x => x.ClinicianId == otherSenior); Assert.True(cover.CoveringTreatment);
        var finances = await Financials(host); var request = Resolve(options, cover);
        var responses = await Task.WhenAll(c.PostAsJsonAsync($"/api/Absences/appointments/{treatment}/resolve", request), c.PostAsJsonAsync($"/api/Absences/appointments/{treatment}/resolve", request)); Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(finances, await Financials(host));
        await DoctorWorkspaceFixture.LoginAsync(c, "access.doctor"); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/Doctor/appointments/{treatment}")).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(c, "clinical.senior"); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/Doctor/appointments/{treatment}")).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(c, "clinical.otherSenior"); var workspace = await c.GetFromJsonAsync<DoctorPatientWorkspace>($"/api/Doctor/appointments/{treatment}"); Assert.True(workspace!.Visit.CoveringTreatment);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/Doctor/appointments/{treatment}/start", new { })).StatusCode);
        host.Clock.Now = new DateTimeOffset(Day.AddDays(1).ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(2));
        await ReceptionAccessFixture.LoginAsync(c); Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Bookings/appointments/{treatment}/status", 4)).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(c, "clinical.otherSenior"); var started = await c.PostAsJsonAsync($"/api/Doctor/appointments/{treatment}/start", new { }); Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync()); workspace = (await started.Content.ReadFromJsonAsync<DoctorPatientWorkspace>())!;
        var note = new NotesMutation(workspace.Notes.Version, "Saved Senior coverage progress", "Stable", "Well tolerated", "Existing treatment performed"); Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Doctor/appointments/{treatment}/notes", note)).StatusCode);
        note = note with { ExpectedVersion = note.ExpectedVersion + 1 }; Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Doctor/appointments/{treatment}/end", note)).StatusCode); Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Doctor/appointments/{treatment}/end", note)).StatusCode);
        workspace = (await c.GetFromJsonAsync<DoctorPatientWorkspace>($"/api/Doctor/appointments/{treatment}"))!; Assert.Null(workspace.Visit.DepartedAt); Assert.Equal("Saved Senior coverage progress", workspace.Notes.ProgressNotes); Assert.Equal(1, workspace.Plan.Completed);
        await ReceptionAccessFixture.LoginAsync(c); Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Bookings/appointments/{treatment}/status", 1)).StatusCode);
        using var verify = host.Services.CreateScope(); var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>(); Assert.Single(await db.AppointmentScheduleChanges.Where(x => x.AppointmentId == treatment).ToListAsync());
    }
    [Fact]
    public async Task Gender_exception_requires_phone_agreement_is_audited_and_contacts_remain_open()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); await Save(c, Absence(await User(host, "access.doctor"))); var options = await Options(c, id); var female = options.Exceptions.First(x => x.Role == "Senior"); var request = Resolve(options, female);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/Bookings/appointments/{id}/schedule", new { ClinicianId = female.ClinicianId, RoomId = female.RoomId, StartTime = Start, UseException = true, PhoneConsent = true, ExceptionReason = "Must not bypass normal reassignment" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", request with { UseException = true, PhoneConsent = false, ExceptionReason = "Patient requested continuity" })).StatusCode);
        foreach (var outcome in new[] { "Declined", "AwaitingResponse" }) { var row = (await Options(c, id)).Appointment; Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/contact", new RecoveryContactRequest(Guid.NewGuid().ToString(), row.Version, row.AbsenceIds[0], outcome, "Synthetic phone follow-up"))).StatusCode); Assert.Equal(outcome, (await c.GetFromJsonAsync<List<RecoveryBooking>>("/api/Absences/affected"))!.Single().ContactStatus); }
        request = request with { UseException = true, PhoneConsent = true, ExceptionReason = "Patient agreed by phone to this eligible Senior" }; Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", request)).StatusCode);
        var history = (await c.GetFromJsonAsync<List<AppointmentScheduleChange>>($"/api/Absences/appointments/{id}/history"))!; var consent = history.Single(x => x.GenderException); Assert.Equal("Phone", consent.ConsentMethod); Assert.NotNull(consent.ConsentRecordedAt); Assert.Equal("Local Receptionist", consent.ActorName); Assert.Equal(3, history.Count); Assert.Empty((await c.GetFromJsonAsync<List<RecoveryBooking>>("/api/Absences/affected"))!);
    }
    [Fact]
    public async Task Senior_assessment_conflict_and_taken_slots_reject_atomically_and_correction_keeps_agreed_moves()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); var senior = await User(host, "clinical.senior"); var assessment = await Assessment(host, senior, room: 2);
        var request = Absence(await User(host, "access.doctor")); var absence = await Save(c, request); var options = await Options(c, id); Assert.DoesNotContain(options.Matching, x => x.ClinicianId == senior);
        var replacement = options.Matching.First(x => x.Role == "Doctor"); var recovery = Resolve(options, replacement); var finances = await Financials(host);
        using (var scope = host.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); db.Appointments.Add(new() { PatientId = 2, DoctorId = replacement.ClinicianId, RoomId = replacement.RoomId, AppointmentType = "Session", StartTime = Start.UtcDateTime, EndTime = Start.UtcDateTime.AddHours(1), Status = AppointmentStatus.Pending }); await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", recovery)).StatusCode); Assert.Equal(finances, await Financials(host));
        var newOptions = await Options(c, id); var moved = newOptions.Matching.First(); Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", Resolve(newOptions, moved))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/{absence.Id}/withdraw", request with { OperationKey = Guid.NewGuid().ToString(), ExpectedVersion = absence.Version })).StatusCode);
        using var verify = host.Services.CreateScope(); var db2 = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var a = await db2.Appointments.SingleAsync(x => x.Id == id); Assert.Equal(moved.ClinicianId, a.DoctorId ?? a.SeniorId); Assert.Equal(Start.UtcDateTime, a.StartTime); Assert.Equal(Start.UtcDateTime, (await db2.Appointments.SingleAsync(x => x.Id == assessment)).StartTime);
    }
    [Fact]
    public async Task Unauthorized_absence_and_recovery_requests_are_denied_and_stale_edits_do_not_overwrite()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/Absences")).StatusCode);
        await ReceptionAccessFixture.LoginAsync(c, "Doctor"); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/Absences/affected")).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(c, "clinical.senior"); Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/Absences", Absence(await User(host, "access.doctor")))).StatusCode);
        await ReceptionAccessFixture.LoginAsync(c); var r = Absence(await User(host, "access.doctor")); var absence = await Save(c, r); Assert.Equal(absence.Id, (await Save(c, r)).Id);
        var correction = r with { OperationKey = Guid.NewGuid().ToString(), ExpectedVersion = absence.Version, FullDay = false, StartTime = new(14, 0), EndTime = new(16, 0) };
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Absences/{absence.Id}", correction)).StatusCode); Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Absences/{absence.Id}", correction)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/Absences/{absence.Id}", correction with { OperationKey = Guid.NewGuid().ToString(), Note = "Stale edit" })).StatusCode);
    }
    [Theory]
    [InlineData("DoctorCapacity")]
    [InlineData("DoctorSameRoom")]
    [InlineData("RoomConflict")]
    [InlineData("PatientConflict")]
    [InlineData("SeniorAssessment")]
    [InlineData("SeniorTreatment")]
    [InlineData("PartialAbsence")]
    public async Task Every_recovery_confirmation_revalidates_clinician_patient_room_and_absence_conflicts(string conflict)
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); await Save(c, Absence(await User(host, "access.doctor"))); var options = await Options(c, id);
        var candidate = options.Matching.First(x => x.Role == (conflict.StartsWith("Senior") ? "Senior" : "Doctor"));
        var request = Resolve(options, candidate); var financial = await Financials(host);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Appointment Extra(int room, string? clinician = null, int patient = 2, string type = "Session") => new() { PatientId = patient, DoctorId = type == "Session" ? clinician : null, SeniorId = type == "Assessment" ? clinician : null, RoomId = room, AppointmentType = type, StartTime = Start.UtcDateTime.AddMinutes(30), EndTime = Start.UtcDateTime.AddMinutes(90), Status = AppointmentStatus.Pending };
            if (conflict == "DoctorCapacity") db.Appointments.AddRange(Extra(2, candidate.ClinicianId), Extra(3, candidate.ClinicianId));
            if (conflict == "DoctorSameRoom") { (await db.Rooms.SingleAsync(r => r.Id == 1)).Capacity = 3; db.Appointments.Add(Extra(1, candidate.ClinicianId)); }
            if (conflict == "RoomConflict") db.Appointments.Add(Extra(1, await User(host, "clinical.otherSenior")));
            if (conflict == "PatientConflict") db.Appointments.Add(Extra(2, await User(host, "clinical.senior"), 1));
            if (conflict == "SeniorAssessment") db.Appointments.Add(Extra(2, candidate.ClinicianId, 2, "Assessment"));
            if (conflict == "SeniorTreatment") { var coverage = Extra(2); coverage.SeniorId = candidate.ClinicianId; db.Appointments.Add(coverage); }
            if (conflict == "PartialAbsence") db.DoctorAbsences.Add(new() { DoctorId = candidate.ClinicianId, AbsentDate = Day, StartsAt = Start.UtcDateTime.AddMinutes(45), EndsAt = Start.UtcDateTime.AddMinutes(75) });
            await db.SaveChangesAsync();
        }
        var response = await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", request); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Equal(financial, await Financials(host));
        using var verify = host.Services.CreateScope(); var db2 = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>(); Assert.Equal(Start.UtcDateTime, (await db2.Appointments.SingleAsync(a => a.Id == id)).StartTime); Assert.Empty(await db2.AppointmentScheduleChanges.ToListAsync());
    }
    [Fact]
    public async Task Reschedule_changes_one_appointment_and_two_reception_edits_cannot_overwrite_each_other()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); var untouchedId = await DoctorWorkspaceFixture.BookAsync(host, day: 1); var r = Absence(await User(host, "access.doctor")); var absence = await Save(c, r);
        var options = (await c.GetFromJsonAsync<RecoveryOptions>($"/api/Absences/appointments/{id}/options?date=2030-02-06"))!; var candidate = options.NearestMatching.First(); var financial = await Financials(host);
        var first = Resolve(options, candidate, "Reschedule"); var second = Resolve(options, options.NearestMatching.Last(), "Reschedule");
        var responses = await Task.WhenAll(c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", first), c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", second)); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict); Assert.Equal(financial, await Financials(host));
        var correction = r with { OperationKey = Guid.NewGuid().ToString(), ExpectedVersion = absence.Version, FullDay = false, StartTime = new(18, 0), EndTime = new(19, 0) }; Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Absences/{absence.Id}", correction)).StatusCode);
        using var verify = host.Services.CreateScope(); var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var changed = await db.Appointments.SingleAsync(a => a.Id == id); Assert.Equal(Day.AddDays(2), DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(changed.StartTime, DateTimeKind.Utc), ClinicCalendar.Zone))); Assert.Equal(Start.UtcDateTime.AddDays(1), (await db.Appointments.SingleAsync(a => a.Id == untouchedId)).StartTime); Assert.Single(await db.AppointmentScheduleChanges.Where(a => a.AppointmentId == id).ToListAsync());
    }
    [Fact]
    public async Task Corrected_absence_recalculates_unresolved_cases_and_preserves_legacy_full_day_semantics()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); var request = Absence(await User(host, "access.doctor")); var absence = await Save(c, request); Assert.Single(absence.Affected);
        var correction = request with { OperationKey = Guid.NewGuid().ToString(), ExpectedVersion = absence.Version, FullDay = false, StartTime = new(14, 0), EndTime = new(16, 0) };
        var saved = await c.PutAsJsonAsync($"/api/Absences/{absence.Id}", correction); Assert.Equal(HttpStatusCode.OK, saved.StatusCode); Assert.Empty((await c.GetFromJsonAsync<List<RecoveryBooking>>("/api/Absences/affected"))!);
        var view = (await saved.Content.ReadFromJsonAsync<AbsenceView>())!; Assert.Equal(2, view.History.Count); Assert.Equal("Correct", view.History[1].Action); Assert.Equal(absence.Start, view.History[1].PreviousStart);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var a = await db.Appointments.SingleAsync(x => x.Id == id); Assert.Equal(Start.UtcDateTime, a.StartTime); Assert.Equal(request.ClinicianId, a.DoctorId);
        var legacy = new DoctorAbsence { DoctorId = request.ClinicianId, AbsentDate = Day }; Assert.True(ClinicianAbsenceRules.Overlaps(legacy, a.StartTime, a.EndTime)); Assert.False(ClinicianAbsenceRules.Overlaps(legacy, Start.AddDays(1).UtcDateTime, Start.AddDays(1).UtcDateTime.AddHours(1)));
    }
    [Fact]
    public async Task Saved_room_capacity_is_respected_and_recovery_retains_nonstandard_original_duration()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); await Save(c, Absence(await User(host, "access.doctor")));
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); (await db.Rooms.SingleAsync(r => r.Id == 1)).Capacity = 2;
            (await db.Appointments.SingleAsync(a => a.Id == id)).EndTime = Start.UtcDateTime.AddMinutes(90);
            db.Appointments.Add(new() { PatientId = 2, DoctorId = await User(host, "clinical.otherdoctor"), RoomId = 1, AppointmentType = "Session", StartTime = Start.UtcDateTime, EndTime = Start.UtcDateTime.AddHours(1), Status = AppointmentStatus.Pending }); await db.SaveChangesAsync();
        }
        var options = await Options(c, id); var senior = options.Matching.First(x => x.Role == "Senior");
        Assert.Equal(1, senior.RoomId); Assert.Equal(TimeSpan.FromMinutes(90), senior.End - senior.Start); Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", Resolve(options, senior))).StatusCode);
        using var verify = host.Services.CreateScope(); var saved = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().Appointments.SingleAsync(a => a.Id == id); Assert.Equal(TimeSpan.FromMinutes(90), saved.EndTime - saved.StartTime);
    }
    [Fact]
    public async Task Covered_Senior_remains_named_and_reschedulable_in_the_existing_patient_profile()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host); using var c = Client(host); await ReceptionAccessFixture.LoginAsync(c);
        var id = await DoctorWorkspaceFixture.BookAsync(host); await Save(c, Absence(await User(host, "access.doctor"))); var options = await Options(c, id); var senior = options.Matching.First(x => x.Role == "Senior");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/Absences/appointments/{id}/resolve", Resolve(options, senior))).StatusCode);
        var profile = (await c.GetFromJsonAsync<PatientProfileData>("/api/Bookings/patients/1/profile"))!; Assert.Equal(senior.Name, profile.Courses.Single().Sessions.Single(s => s.AppointmentId == id).DoctorName);
        var available = (await c.GetFromJsonAsync<RescheduleAvailability>($"/api/Bookings/appointments/{id}/reschedule-slots?date=2030-02-06"))!; Assert.Equal(senior.Name, available.ClinicianName); var slot = available.Slots.First(); var finances = await Financials(host);
        var request = new RescheduleSessionRequest { OperationKey = Guid.NewGuid().ToString(), ExpectedVersion = available.Version, StartTime = new DateTimeOffset(DateTime.SpecifyKind(slot.StartTime, DateTimeKind.Utc)), RoomId = slot.RoomId, Reason = "Patient agreed to a later covered session" };
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/Bookings/appointments/{id}/reschedule", request)).StatusCode); Assert.Equal(finances, await Financials(host));
        using var verify = host.Services.CreateScope(); var saved = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().Appointments.SingleAsync(a => a.Id == id); Assert.Equal(senior.ClinicianId, saved.SeniorId); Assert.Null(saved.DoctorId);
    }
}
