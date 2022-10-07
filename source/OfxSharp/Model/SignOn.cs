using System;
using System.Globalization;
using System.Xml;

namespace OfxSharp
{
    /// <summary><c>&lt;SIGNONMSGSRSV1&gt;</c> with child <c>&lt;SONRS&gt;</c> and more.</summary>
    public class SignOnResponse
    {
        /// <param name="signonMsgsRsV1">Required. Must be an <c>&lt;SIGNONMSGSRSV1&gt;</c> element.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static SignOnResponse FromXmlElement( XmlElement signonMsgsRsV1 )
        {
            if( signonMsgsRsV1 is null ) throw new ArgumentNullException( nameof( signonMsgsRsV1 ) );

            _ = signonMsgsRsV1.AssertIsElement( "SIGNONMSGSRSV1" );

            XmlElement signOnResponse = signonMsgsRsV1.RequireSingleElementChild( "SONRS"  );
            XmlElement status         = signOnResponse.RequireSingleElementChild( "STATUS" );

            return new SignOnResponse(
                statusCode        : status        .RequireSingleElementChildText( "CODE"     ).RequireParseInt32Inv(),
                statusSeverity    : status        .RequireSingleElementChildText( "SEVERITY" ),
                dtServer          : signOnResponse.RequireSingleElementChildText( "DTSERVER" ).RequireOptionalParseOfxDateTime(),
                language          : signOnResponse.RequireSingleElementChildText( "LANGUAGE" ),

                country           : signOnResponse.GetSingleElementChildTextOrNull( "COUNTRY" ),
                institution       : FinancialInstitution.FromXmlElementOrNull( signOnResponse.GetSingleElementChildOrNull("FI") ),
                profileLastUpdated: signOnResponse.GetSingleElementChildTextOrNull( "DTPROFUP" ).RequireOptionalParseOfxDateTime(),
                accountLastUpdated: signOnResponse.GetSingleElementChildTextOrNull( "DTACCTUP" ).RequireOptionalParseOfxDateTime(),

                // <INTU.BID> and <INTU.USERID> are in *.qfx files, not *.ofx files.
                intuBId           : signOnResponse.GetSingleElementChildOrNull    ( "INTU.BID"   , allowDotsInElementName: true )?.RequireSingleTextChildNode() ?? null,
                intuUserId        : signOnResponse.GetSingleElementChildOrNull    ( "INTU.USERID", allowDotsInElementName: true )?.RequireSingleTextChildNode() ?? null
            );
        }

        public SignOnResponse(
            Int32                statusCode,
            String               statusSeverity,
            DateTimeOffset?      dtServer,
            String               language,

            String               country            = null,
            FinancialInstitution institution        = null,
            DateTimeOffset?      profileLastUpdated = null,
            DateTimeOffset?      accountLastUpdated = null,
            String               intuBId            = null,
            String               intuUserId         = null
        )
        {
            // Required:
            this.StatusCode         = statusCode;
            this.StatusSeverity     = statusSeverity;
            this.DTServer           = dtServer;
            this.Language           = language;

            // Optional:
            this.Country            = country;
            this.Institution        = institution;
            this.ProfileLastUpdated = profileLastUpdated;
            this.AccountLastUpdated = accountLastUpdated;
            this.IntuBId            = intuBId;
            this.IntuUserId         = intuUserId;
        }

        #region OFX 1.6 Required members

        /// <summary>Required.<br />OFX/SIGNONMSGSRSV1/SONRS/STATUS/CODE</summary>
        public int StatusCode { get; }

        /// <summary>Required.<br />OFX/SIGNONMSGSRSV1/SONRS/STATUS/SEVERITY</summary>
        public string StatusSeverity { get; }

        /// <summary>Required. All-zero (i.e. null) values accepted)<br />OFX/SIGNONMSGSRSV1/SONRS/DTSERVER</summary>
        public DateTimeOffset? DTServer { get; }

        /// <summary>
        /// Required. OFX 1.6 states values are ISO-639 three-letter codes, e.g. &quot;ENG&quot; for English (but doesn't say if that's <c>en-US</c> or <c>en-GB</c>, any idea?) and &quot;POR&quot; for Portuguese.<br />
        /// This value can be used to infer how numeric amounts should be parsed w.r.t. <see cref="System.Globalization.CultureInfo.NumberFormat"/><br />
        /// <c>OFX/SIGNONMSGSRSV1/SONRS/LANGUAGE</c>
        /// </summary>
        public string Language { get; }

        #endregion

        #region OFX 1.6 Optional members and extensions

        /// <summary>
        /// Optional. Can be <see langword="null"/>.<br />
        /// The spec says: &quot;Specific country system used for the requests: 3-letter country code from ISO/DIS-3166. If this element is not present, the country system is USA.&quot;<br />
        /// <c>OFX/SIGNONMSGSRSV1/SONRS/COUNTRY</c></summary>
        public string Country { get; }

        /// <summary>Optional. Can be <see langword="null"/>.<br /><c>OFX/SIGNONMSGSRSV1/SONRS/FI</c></summary>
        public FinancialInstitution Institution { get; }

        /// <summary>Optional. Can be <see langword="null"/>.<br /><c>OFX/SIGNONMSGSRSV1/SONRS/DTPROFUP</c><br >&quot;Date and time of last update to profile information for any service supported by this FI&quot;</summary>
        public DateTimeOffset? ProfileLastUpdated { get; }

        /// <summary>Optional. Can be <see langword="null"/>.<br /><c>OFX/SIGNONMSGSRSV1/SONRS/DTACCTUP</c><br />&quot;Date and time of last update to account information (see Chapter8, “Activation &amp; Account Information”)&quot;</summary>
        public DateTimeOffset? AccountLastUpdated { get; }

        /// <summary>Intuit BankId (proprietary to Quicken/Quickbooks).<br />Can be null.<br /><c>OFX/SIGNONMSGSRSV1/SONRS/INTU.BID</c></summary>
        public string IntuBId { get; }

        /// <summary>Intuit UserId (proprietary to Quicken/Quickbooks).<br />Can be null.<br /><c>OFX/SIGNONMSGSRSV1/SONRS/INTU.USERID</c></summary>
        public string IntuUserId { get; }

        #endregion
    }
}
