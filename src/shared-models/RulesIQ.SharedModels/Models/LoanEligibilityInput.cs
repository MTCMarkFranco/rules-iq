using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// Input model for the Canadian Loan Eligibility RulesEngine workflow.
/// Properties match the entity model defined in the meta-contract.
/// </summary>
public sealed class LoanEligibilityInput
{
    public int Age { get; set; }
    public string Province { get; set; } = string.Empty;
    public string ResidencyStatus { get; set; } = string.Empty;
    public decimal AnnualIncome { get; set; }
    public decimal GDS { get; set; }
    public decimal TDS { get; set; }
    public int CreditScore { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public int EmploymentDurationMonths { get; set; }
    public decimal LoanAmount { get; set; }
    public decimal PropertyValue { get; set; }
    public decimal DownPaymentPercent { get; set; }
    public decimal LTV { get; set; }
    public string LoanType { get; set; } = string.Empty;
    public string LenderType { get; set; } = string.Empty;

    /// <summary>
    /// Returns the canonical test persona from the meta-contract.
    /// </summary>
    public static LoanEligibilityInput CanonicalTestPersona => new()
    {
        Age = 34,
        Province = "ON",
        ResidencyStatus = "PermanentResident",
        AnnualIncome = 92000.00m,
        GDS = 41.5m,
        TDS = 43.0m,
        CreditScore = 710,
        EmploymentStatus = "Employed",
        EmploymentDurationMonths = 28,
        LoanAmount = 485000.00m,
        PropertyValue = 625000.00m,
        DownPaymentPercent = 22.4m,
        LTV = 77.6m,
        LoanType = "Mortgage",
        LenderType = "FederallyRegulated"
    };
}
