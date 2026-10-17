using Microsoft.AspNetCore.Http;
using PTCenter.Domain.Errors;

public static class BillingErrors
{
    public static readonly Error InvoiceNotFound =
        new(
            "Billing.InvoiceNotFound",
            "Invoice was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error PatientNotFound =
        new(
            "Billing.PatientNotFound",
            "Patient was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error AppointmentNotFound =
        new(
            "Billing.AppointmentNotFound",
            "Appointment was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error InvalidAmount =
        new(
            "Billing.InvalidAmount",
            "Amount must be greater than zero.",
            StatusCodes.Status400BadRequest);

  

 
    public static readonly Error InvoiceHasPayments =
        new(
            "Billing.InvoiceHasPayments",
            "Cannot delete an invoice that has payments.",
            StatusCodes.Status409Conflict);
    public static readonly Error InvoiceCannotReceivePayment =
        new(
            "Billing.InvoiceCannotReceivePayment",
            "Cannot add a payment to an invoice that is cancelled or waived.",
            StatusCodes.Status409Conflict);
   

    public static readonly Error PaymentExceedsRemainingAmount
        = new(
            "Billing.PaymentExceedsRemainingAmount",
            "Payment amount exceeds the remaining invoice balance.",
            StatusCodes.Status400BadRequest);
    public static readonly Error PaidInvoiceCannotBeUpdated
        = new(
            "Billing.PaidInvoiceCannotBeUpdated",
            "Cannot update an invoice that has been paid.",
            StatusCodes.Status409Conflict);


}