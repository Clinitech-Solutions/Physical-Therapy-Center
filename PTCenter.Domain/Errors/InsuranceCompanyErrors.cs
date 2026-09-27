using Microsoft.AspNetCore.Http;
using PTCenter.Domain.Errors;
using System;
using System.Collections.Generic;
using System.Text;

public static class InsuranceCompanyErrors
{
    public static readonly Error InsuranceCompanyNotFound =
        new(
            "InsuranceCompany.NotFound",
            "Insurance company was not found.",StatusCodes.Status404NotFound);

    public static readonly Error InsuranceCompanyAlreadyExists =
        new(
            "InsuranceCompany.AlreadyExists",
            "Insurance company already exists.",StatusCodes.Status400BadRequest);
}

