using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace OfxSharp
{
    /// <summary>Flattened view of STMTTRNRS and STMTRS. 11.4.1.2 Response &lt;STMTRS&gt;<br />
    /// &quot;The &lt;STMTRS&gt; response must appear within a &lt;STMTTRNRS&gt; transaction wrapper.&quot; (the &quot;transaction&quot; refers to the OFX request/response transaction - not a bank transaction).</summary>
    public class OfxStatementResponse
    {
        /// <param name="stmtrnrs">Required. Cannot be <see langword="null"/>. Must be a <c>&lt;BANKMSGSRSV1&gt;</c> element.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static OfxStatementResponse FromSTMTTRNRS( XmlElement stmtrnrs, CultureInfo culture )
        {
            if( stmtrnrs is null ) throw new ArgumentNullException( nameof( stmtrnrs ) );
            if( culture  is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            _ = stmtrnrs.AssertIsElement( "STMTTRNRS", parentElementName: "BANKMSGSRSV1" );

            XmlElement stmtrs    = stmtrnrs.RequireSingleElementChild("STMTRS");
            XmlElement transList = stmtrs  .RequireSingleElementChild("BANKTRANLIST");

            //

            String defaultCurrency = stmtrs.RequireSingleElementChildText("CURDEF");

            return new OfxStatementResponse(
                trnUid           : stmtrnrs.RequireSingleElementChildText("TRNUID").RequireParseInt32(),
                responseStatus   : OfxStatus.FromXmlElement( stmtrnrs.RequireSingleElementChild("STATUS") ),
                defaultCurrency  : defaultCurrency,
                accountFrom      : Account.FromXmlElementOrNull( stmtrs.GetSingleElementChildOrNull("BANKACCTFROM") ),
                transactionsStart: transList.RequireSingleElementChildText("DTSTART").RequireParseOfxDateTime(),
                transactionsEnd  : transList.RequireSingleElementChildText("DTEND"  ).RequireParseOfxDateTime(),
                transactions     : GetTransactions( transList, defaultCurrency, culture ),
                ledgerBalance    : Balance.FromXmlElementOrNull( stmtrs.GetSingleElementChildOrNull("LEDGERBAL"), culture ),
                availableBalance : Balance.FromXmlElementOrNull( stmtrs.GetSingleElementChildOrNull("AVAILBAL" ), culture )
            );
        }

        /// <summary>For Chase's OFX 1.6-violating QFX files, which have <c>&lt;CREDITCARDMSGSRSV1&gt;&lt;CCSTMTTRNRS&gt;&lt;CCSTMTRS&gt;</c> (and other differences) instead of <c>&lt;BANKMSGSRSV1&gt;&lt;STMTTRNRS&gt;&lt;STMTRS&gt;</c>.</summary>
        /// <param name="ccStmtTrnRs">Required. Cannot be <see langword="null"/>. Must be a <c>&lt;CCSTMTTRNRS&gt;</c> element that's an immediate child of a <c>&lt;CREDITCARDMSGSRSV1&gt;</c>.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static OfxStatementResponse FromCCSTMTTRNRS( XmlElement ccStmtTrnRs, CultureInfo culture )
        {
            _ = ccStmtTrnRs.AssertIsElement( "CCSTMTTRNRS", parentElementName: "CREDITCARDMSGSRSV1" );

            XmlElement stmtrs    = ccStmtTrnRs.RequireSingleElementChild("CCSTMTRS");
            XmlElement transList = stmtrs  .RequireSingleElementChild("BANKTRANLIST");

            //

            String defaultCurrency = stmtrs.RequireSingleElementChildText("CURDEF");

            return new OfxStatementResponse(
                trnUid           : ccStmtTrnRs.RequireSingleElementChildText("TRNUID").RequireParseInt32(),
                responseStatus   : OfxStatus.FromXmlElement( ccStmtTrnRs.RequireSingleElementChild("STATUS") ),
                defaultCurrency  : defaultCurrency,
                accountFrom      : Account.FromXmlElementOrNull( stmtrs.GetSingleElementChildOrNull("CCACCTFROM") ),
                transactionsStart: transList.RequireSingleElementChildText( "DTSTART").RequireParseOfxDateTime(),
                transactionsEnd  : transList.RequireSingleElementChildText( "DTEND"  ).RequireParseOfxDateTime(),
                transactions     : GetTransactions( transList, defaultCurrency, culture ),
                ledgerBalance    : Balance.FromXmlElementOrNull( stmtrs.GetSingleElementChildOrNull("LEDGERBAL"), culture ),
                availableBalance : Balance.FromXmlElementOrNull( stmtrs.GetSingleElementChildOrNull("AVAILBAL" ), culture )
            );
        }

        /// <param name="bankTranList">Required. Cannot be <see langword="null"/>. Must be a <c>&lt;BANKTRANLIST&gt;</c> element.</param>
        /// <param name="defaultCurrency">Required. Cannot be <see langword="null"/>.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static IEnumerable<Transaction> GetTransactions( XmlElement bankTranList, string defaultCurrency, CultureInfo culture )
        {
            if( bankTranList     is null ) throw new ArgumentNullException( nameof( bankTranList ) );
            if( defaultCurrency  is null ) throw new ArgumentNullException( nameof( defaultCurrency ) );
            if( culture          is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            _ = bankTranList.AssertIsElement("BANKTRANLIST"); // <-- This appears in both BANKMSGSRSV1 and CREDITCARDMSGSRSV1 btw.

            foreach( XmlElement stmtTrn in bankTranList.GetChildNodes( "STMTTRN" ).Cast<XmlElement>() )
            {
                yield return new Transaction( stmtTrn: stmtTrn, defaultCurrency, culture );
            }
        }

        public OfxStatementResponse(
            Int32                    trnUid,
            OfxStatus                responseStatus,
            String                   defaultCurrency,
            Account                  accountFrom,
            DateTimeOffset           transactionsStart,
            DateTimeOffset           transactionsEnd,
            IEnumerable<Transaction> transactions,
            Balance                  ledgerBalance,
            Balance                  availableBalance
        )
        {
            this.OfxTransactionUniqueId = trnUid;
            this.ResponseStatus         = responseStatus   ?? throw new ArgumentNullException( nameof( responseStatus ) );
            this.DefaultCurrency        = defaultCurrency  ?? throw new ArgumentNullException( nameof( defaultCurrency ) );
            this.AccountFrom            = accountFrom      ?? throw new ArgumentNullException( nameof( accountFrom ) );
            this.TransactionsStart      = transactionsStart;
            this.TransactionsEnd        = transactionsEnd;
            this.LedgerBalance          = ledgerBalance    ?? throw new ArgumentNullException( nameof( ledgerBalance ) );
            this.AvailableBalance       = availableBalance;

            this.Transactions.AddRange( transactions );
        }

        /// <summary>STMTTRNRS/TRNUID (OFX Request/Response Transaction ID - this is unrelated to bank transactions).</summary>
        public Int32 OfxTransactionUniqueId { get; }

        /// <summary>STMTTRNRS/STATUS</summary>
        public OfxStatus ResponseStatus { get; }

        /// <summary>STMTTRNRS/STMTRS/CURDEF</summary>
        public String DefaultCurrency { get; }

        /// <summary>STMTTRNRS/STMTRS/BANKACCTFROM</summary>
        public Account AccountFrom { get; }

        /// <summary>STMTTRNRS/STMTRS/BANKTRANLIST/DTSTART</summary>
        public DateTimeOffset TransactionsStart { get; }

        /// <summary>STMTTRNRS/STMTRS/BANKTRANLIST/DTEND</summary>
        public DateTimeOffset TransactionsEnd   { get; }

        /// <summary>STMTTRNRS/STMTRS/BANKTRANLIST</summary>
        public List<Transaction> Transactions { get; } = new List<Transaction>();

        /// <summary>STMTTRNRS/STMTRS/LEDGERBAL. Required.</summary>
        public Balance LedgerBalance { get; }

        /// <summary>STMTTRNRS/STMTRS/AVAILBAL. Optional. Can be null.</summary>
        public Balance AvailableBalance { get; }
    }
}
