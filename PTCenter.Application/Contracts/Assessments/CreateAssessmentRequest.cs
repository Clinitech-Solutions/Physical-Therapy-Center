using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Assessments

{
    public class CreateAssessmentRequest
    {
        public int PatientId { get; set; }

        public string SeniorId { get; set; } = null!;

        public string Diagnosis { get; set; } = null!;

        public string ConditionType { get; set; } = null!;

        public string Priority { get; set; } = null!;

        public string? Notes { get; set; }

        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;

        public Status Status { get; set; } = Status.Low;
    }
}
