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
        /// <param name="stmtTrnRs">Required. Cannot be <see langword="null"/>. Must be a <c>&lt;BANKMSGSRSV1&gt;&lt;STMTTRNRS&gt;</c> element.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static OfxStatementResponse FromSTMTTRNRS( XmlElement stmtTrnRs, CultureInfo culture )
        {
            if( stmtTrnRs is null ) throw new ArgumentNullException( nameof( stmtTrnRs ) );
            if( culture   is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            _ = stmtTrnRs.AssertIsElement( "STMTTRNRS", parentElementName: "BANKMSGSRSV1" );

            XmlElement  status          = stmtTrnRs.RequireSingleElementChild  ( "STATUS"       );
            XmlElement  stmtrs          = stmtTrnRs.RequireSingleElementChild  ( "STMTRS"       );
            XmlElement  transList       = stmtrs   .RequireSingleElementChild  ( "BANKTRANLIST" );
            XmlElement  bankAccountFrom = stmtrs   .RequireSingleElementChild  ( "BANKACCTFROM" );
            XmlElement  ledgerBal       = stmtrs   .RequireSingleElementChild  ( "LEDGERBAL"    );
            XmlElement? availBal        = stmtrs   .GetSingleElementChildOrNull( "AVAILBAL"     );

            String trnUid          = stmtTrnRs.RequireSingleElementChildText( "TRNUID" );
            String defaultCurrency = stmtrs   .RequireSingleElementChildText( "CURDEF" );

            return new OfxStatementResponse(
                trnUid           : trnUid,
                responseStatus   : OfxStatus.FromXmlElement( status ),
                defaultCurrency  : defaultCurrency,
                accountFrom      : Account.FromXmlElement( bankAccountFrom ),
                transactionsStart: transList.RequireSingleElementChildText( "DTSTART" ).RequireParseOfxDateTime(),
                transactionsEnd  : transList.RequireSingleElementChildText( "DTEND"   ).RequireParseOfxDateTime(),
                transactions     : GetTransactions( transList, defaultCurrency, culture ),
                ledgerBalance    : Balance.FromXmlElement     ( ledgerBal, culture ),
                availableBalance : Balance.FromXmlElementOrNull( availBal, culture )
            );
        }

        /// <summary>For Chase's OFX 1.6-violating QFX files, which have <c>&lt;CREDITCARDMSGSRSV1&gt;&lt;CCSTMTTRNRS&gt;&lt;CCSTMTRS&gt;</c> (and other differences) instead of <c>&lt;BANKMSGSRSV1&gt;&lt;STMTTRNRS&gt;&lt;STMTRS&gt;</c>.</summary>
        /// <param name="ccStmtTrnRs">Required. Cannot be <see langword="null"/>. Must be a <c>&lt;CCSTMTTRNRS&gt;</c> element that's an immediate child of a <c>&lt;CREDITCARDMSGSRSV1&gt;</c>.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static OfxStatementResponse FromCCSTMTTRNRS( XmlElement ccStmtTrnRs, CultureInfo culture )
        {
            _ = ccStmtTrnRs.AssertIsElement( "CCSTMTTRNRS", parentElementName: "CREDITCARDMSGSRSV1" );

            XmlElement  status     = ccStmtTrnRs.RequireSingleElementChild  ( "STATUS"       );
            XmlElement  stmtrs     = ccStmtTrnRs.RequireSingleElementChild  ( "CCSTMTRS"     );
            XmlElement  transList  = stmtrs     .RequireSingleElementChild  ( "BANKTRANLIST" );
            XmlElement  ccAcctFrom = stmtrs     .RequireSingleElementChild  ( "CCACCTFROM"   );
            XmlElement  ledgerBal  = stmtrs     .RequireSingleElementChild  ( "LEDGERBAL"    );
            XmlElement? availBal   = stmtrs     .GetSingleElementChildOrNull( "AVAILBAL"     );


            //

            String defaultCurrency = stmtrs.RequireSingleElementChildText("CURDEF");

            return new OfxStatementResponse(
                trnUid           : ccStmtTrnRs.RequireSingleElementChildText( "TRNUID" ),
                responseStatus   : OfxStatus.FromXmlElement( status ),
                defaultCurrency  : defaultCurrency,
                accountFrom      : Account.FromXmlElement( ccAcctFrom ),
                transactionsStart: transList.RequireSingleElementChildText( "DTSTART").RequireParseOfxDateTime(),
                transactionsEnd  : transList.RequireSingleElementChildText( "DTEND"  ).RequireParseOfxDateTime(),
                transactions     : GetTransactions( transList, defaultCurrency, culture ),
                ledgerBalance    : Balance.FromXmlElement      ( ledgerBal, culture ),
                availableBalance : Balance.FromXmlElementOrNull( availBal , culture )
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

            foreach( XmlElement stmtTrn in bankTranList.GetChildElements( "STMTTRN" ) )
            {
                yield return new Transaction( stmtTrn: stmtTrn, defaultCurrency, culture );
            }
        }

        public OfxStatementResponse(
            String                   trnUid,
            OfxStatus                responseStatus,
            String                   defaultCurrency,
            Account                  accountFrom,
            DateTimeOffset           transactionsStart,
            DateTimeOffset           transactionsEnd,
            IEnumerable<Transaction> transactions,
            Balance                  ledgerBalance,
            Balance?                 availableBalance
        )
        {
            this.OfxTransactionUniqueId = trnUid;
            this.ResponseStatus         = responseStatus   ?? throw new ArgumentNullException( nameof(responseStatus) );
            this.DefaultCurrency        = defaultCurrency  ?? throw new ArgumentNullException( nameof(defaultCurrency) );
            this.AccountFrom            = accountFrom      ?? throw new ArgumentNullException( nameof(accountFrom) );
            this.TransactionsStart      = transactionsStart;
            this.TransactionsEnd        = transactionsEnd;
            this.LedgerBalance          = ledgerBalance    ?? throw new ArgumentNullException( nameof(ledgerBalance) );
            this.AvailableBalance       = availableBalance;

            if( transactions is null ) throw new ArgumentNullException( nameof(transactions) );

            this.transactionsList.AddRange( transactions );
        }

        /// <summary>STMTTRNRS/TRNUID (OFX Request/Response Transaction ID - this is unrelated to bank transactions).<br />&quot;Client-Assigned Transaction UID&quot; - Described as an alphanumeric field (i.e. a <see cref="string"/>) with a maximum possible length of 36 chars. Values consisting of a single zero character have special meaning (in some OFX contexts, but not in .ofx files, surely?) and are valid.</summary>
        public String OfxTransactionUniqueId { get; }

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

        private readonly List<Transaction> transactionsList = new List<Transaction>();

        /// <summary>STMTTRNRS/STMTRS/BANKTRANLIST</summary>
        public IReadOnlyList<Transaction> Transactions => this.transactionsList;

        /// <summary>STMTTRNRS/STMTRS/LEDGERBAL. Required.</summary>
        public Balance LedgerBalance { get; }

        /// <summary>STMTTRNRS/STMTRS/AVAILBAL. Optional. Can be null.</summary>
        public Balance? AvailableBalance { get; }
    }
}
