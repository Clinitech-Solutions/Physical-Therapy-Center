using System;
using System.Collections.Generic;
using System.Text;

  namespace PTCenter.Application.Contracts.Patients;

    public class PatientQueryRequest
    {
        public string? Search { get; set; }

        public string? PaymentType { get; set; }

        public int? InsuranceCompanyId { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

