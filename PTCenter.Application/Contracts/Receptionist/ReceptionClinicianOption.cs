namespace PTCenter.Application.Contracts.Receptionist;

public record ReceptionWorkingHours(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public record ReceptionClinicianOption(string Id, string Name, string Gender, List<string> Roles,
    int CurrentLoad, List<ReceptionWorkingHours> WorkingHours, List<DateOnly> AbsenceDates);
