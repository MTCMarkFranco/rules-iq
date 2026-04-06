namespace RulesIQ.SharedModels.Constants;

public static class ProvinceConstants
{
    public static readonly string[] AgeOfMajority19Provinces =
        ["BC", "NB", "NS", "NL", "NT", "NU", "YT"];

    public const int DefaultAgeOfMajority = 18;
    public const int ExtendedAgeOfMajority = 19;

    public static readonly string[] AllProvinces =
        ["AB", "BC", "MB", "NB", "NL", "NS", "NT", "NU", "ON", "PE", "QC", "SK", "YT"];
}

public static class RulesConstants
{
    public const string DefaultRuleExpressionType = "LambdaExpression";
    public const string DefaultWorkflowName = "CanadianLoanEligibility";
    public const decimal MaxGdsRatio = 39m;
    public const decimal MaxTdsRatio = 44m;
    public const int MinCreditScore = 650;
    public const int MinEmploymentMonths = 6;
    public const decimal MinAnnualIncome = 25000m;
}

public static class IndexConstants
{
    public const string IndexName = "idx-rules-iq";
    public const string DataSourceName = "ds-policy-documents";
    public const string SkillsetName = "ss-rule-extraction";
    public const string IndexerName = "ixr-policy-rules";
    public const string BlobContainerName = "policy-documents";
    public const int EmbeddingDimensions = 3072;
}
