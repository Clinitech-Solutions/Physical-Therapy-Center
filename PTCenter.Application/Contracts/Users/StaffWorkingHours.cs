namespace PTCenter.Application.Contracts.Users;

public record StaffWorkingHours(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
