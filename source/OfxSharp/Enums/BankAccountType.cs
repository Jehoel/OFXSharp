using System.Runtime.Serialization;
using System.ComponentModel;

namespace OfxSharp
{
    /// <summary>Combines <c>ACCOUNTENUM</c> and <c>ACCOUNTENUM2</c>.</summary>
    /// <remarks>All members, except <see cref="CMA"/>, are present in both <c>ACCOUNTENUM</c> and <c>ACCOUNTENUM2</c> (while <see cref="CMA"/> is exclusive to <c>ACCOUNTENUM2</c>).</remarks>
    public enum BankAccountType
    {
        [EnumMember( Value = "CHECKING" )]
        [Description("Checking Account")]
        CHECKING,

        [EnumMember( Value = "SAVINGS" )]
        [Description("Savings Account")]
        SAVINGS,

        [EnumMember( Value = "MONEYMRKT" )]
        [Description("Money Market Account")]
        MONEYMRKT,

        [EnumMember( Value = "CREDITLINE" )]
        [Description("Line of Credit")]
        CREDITLINE,

        [EnumMember( Value = "NA" )]
        NA,

        [EnumMember( Value = "HOMELOAN" )]
        [Description("Home Loan")]
        HOMELOAN,

        [EnumMember( Value = "CMA" )]
        [Description("CMA")]
        CMA
    }
}
