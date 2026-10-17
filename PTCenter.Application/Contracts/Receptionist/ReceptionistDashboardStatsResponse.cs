using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Receptionist;

public class ReceptionistDashboardStatsResponse
{
    public int TodaysSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int PendingPayments { get; set; }
    public decimal TotalExpectedToday { get; set; }
    public decimal TotalCollectedToday { get; set; }
    public double CollectionRate { get; set; }
}

