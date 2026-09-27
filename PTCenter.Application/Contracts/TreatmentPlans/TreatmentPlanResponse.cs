using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.TreatmentPlans;

   public class TreatmentPlanResponse
    {
        public int Id { get; set; }

        public int AssessmentId { get; set; }

        public string DoctorId { get; set; } = null!;

        public string DoctorName { get; set; } = null!;

        public int NumberOfSessions { get; set; }

        public int CompletedSessions { get; set; }

        public decimal ProgressPercentage { get; set; }

        public int FrequencyPerWeek { get; set; }

        public string TreatmentType { get; set; } = null!;

        public string? Exercises { get; set; }

        public StatusPlan Status { get; set; }
    }
