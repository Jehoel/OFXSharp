using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace OfxSharp.NETCore.Tests
{
#pragma warning disable IDE0058 // Expression value is never used

    public class OfxFileParseTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_read_ITAU_statements()
        {
            OfxDocument ofx = OfxDocumentReader.FromSgmlFile( filePath: @"Files\itau.ofx" );

            ofx.HasSingleStatement( out SingleStatementOfxDocument ofxDocument ).Should().BeTrue();

            ofxDocument.Should().NotBeNull();
            ofxDocument.StatementStart.Should().Be(new DateTimeOffset(2013, 12,  5, 10, 0, 0, TimeSpan.FromHours(-3))); // "20131205100000[-03:EST]" -> 2013-12-05 10:00:00-03:00
            ofxDocument.StatementEnd  .Should().Be(new DateTimeOffset(2014,  2, 28, 10, 0, 0, TimeSpan.FromHours(-3))); // "20140228100000[-03:EST]" -> 2014-02-28 10:00:00-03:00

            Account.BankAccount acc = ofxDocument.Account.Should().BeOfType<Account.BankAccount>().Subject;
            acc.AccountId.Trim().Should().Be("9999999999");
            acc.BankId   .Trim().Should().Be("0341");

            ofxDocument.Transactions.Count.Should().Be(3);
            ofxDocument.Transactions.Sum(x => x.Amount).Should().Be(-644.44M);
        }

        [Test]
        public void Should_read_Santander_statements()
        {
            OfxDocument ofx = OfxDocumentReader.FromSgmlFile( filePath: @"Files\santander.ofx" );
            ofx.HasSingleStatement( out SingleStatementOfxDocument ofxDocument ).Should().BeTrue();

            ofxDocument.Should().NotBeNull();
            ofxDocument.StatementStart.Should().Be(new DateTimeOffset(2014, 2, 3, 18, 22, 51, TimeSpan.FromHours(-3))); // "20140203182251[-3:GMT]" -> 2014-02-03 18:22:51-03:00
            ofxDocument.StatementEnd  .Should().Be(new DateTimeOffset(2014, 2, 3, 18, 22, 51, TimeSpan.FromHours(-3))); // "20140203182251[-3:GMT]" -> 2014-02-03 18:22:51-03:00

            Account.BankAccount acc = ofxDocument.Account.Should().BeOfType<Account.BankAccount>().Subject;
            acc.AccountId.Trim().Should().Be("9999999999999");
            acc.BankId   .Trim().Should().Be("033");

            ofxDocument.Transactions.Count.Should().Be(3);
            ofxDocument.Transactions.Sum(x => x.Amount).Should().Be(-22566.44M);
        }

        [Test]
        public void Should_read_Bradseco_statements()
        {
            OfxDocument ofx = OfxDocumentReader.FromSgmlFile( filePath: @"Files\bradesco.ofx" );
            ofx.HasSingleStatement( out SingleStatementOfxDocument ofxDocument ).Should().BeTrue();

            ofxDocument.Should().NotBeNull();
            ofxDocument.StatementStart.Should().Be( new DateTimeOffset(2019, 5, 9, 12, 0, 0, TimeSpan.Zero ) ); // "20190509120000" -> 2019-05-09 12:00:00
            ofxDocument.StatementEnd  .Should().Be( new DateTimeOffset(2019, 5, 9, 12, 0, 0, TimeSpan.Zero ) ); // "20190509120000" -> 2019-05-09 12:00:00

            Account.BankAccount acc = ofxDocument.Account.Should().BeOfType<Account.BankAccount>().Subject;
            acc.AccountId.Trim().Should().Be("99999");
            acc.BankId   .Trim().Should().Be("0237");

            ofxDocument.Transactions.Count.Should().Be(3);
//          ofxDocument.Transactions.Sum(x => x.Amount).Should().Be(200755M); // <-- This is wrong... I hope I didn't get the test-value from the debugger...

            DateTimeOffset _20190430120000 = new DateTimeOffset(2019, 4, 30, 12, 0, 0, TimeSpan.Zero );

            ofxDocument.Transactions[0].TransType    .Should().Be( OfxTransactionType.CREDIT );
            ofxDocument.Transactions[0].Date         .Should().Be( _20190430120000 );
            ofxDocument.Transactions[0].Amount       .Should().Be( 10.0M ); // <-- This is to ensure that the "10,00" in the OFX file is parsed as "10.0M" as it's using continential european decimal formatting (i.e. commas for the radix point instead of a dot - note that digit-grouping isn't used in OFX (I think?) so digit-grouping chars (`,` in en-US but `.` in fr-FR) don't matter.
            ofxDocument.Transactions[0].TransactionId.Should().Be( "N1013F" );
            ofxDocument.Transactions[0].CheckNum     .Should().Be( "3243801" );
            ofxDocument.Transactions[0].Memo         .Should().Be( "RESGATE INVEST" );

            ofxDocument.Transactions[1].TransType    .Should().Be( OfxTransactionType.CREDIT );
            ofxDocument.Transactions[1].Date         .Should().Be( _20190430120000 );
            ofxDocument.Transactions[1].Amount       .Should().Be( 24783.31M ); // Stored as `<TRNAMT>24783,31`
            ofxDocument.Transactions[1].TransactionId.Should().Be( "N10153" );
            ofxDocument.Transactions[1].CheckNum     .Should().Be( "3243801" );
            ofxDocument.Transactions[1].Memo         .Should().Be( "RESGATE INVEST" ); // Yeah, it's the same.

            ofxDocument.Transactions[2].TransType    .Should().Be( OfxTransactionType.DEBIT );
            ofxDocument.Transactions[2].Date         .Should().Be( _20190430120000 );
            ofxDocument.Transactions[2].Amount       .Should().Be( -22785.76M ) ;
            ofxDocument.Transactions[2].TransactionId.Should().Be( "N10167" );
            ofxDocument.Transactions[2].CheckNum     .Should().Be( "936909" );
            ofxDocument.Transactions[2].Memo         .Should().Be( "APLICACAO EM FUNDOS" );
        }

        [Test]
        public void Should_read_SecondLuddite_statements()
        {
            OfxDocument ofx = OfxDocumentReader.FromSgmlFile( filePath: @"Files\secondluddite.ofx" );
            ofx.HasSingleStatement( out _ ).Should().BeFalse();

            ofx.SignOn.StatusCode.Should().Be( 0 );
            ofx.SignOn.StatusSeverity.Should().Be( "INFO" );
            ofx.SignOn.DTServer.Value.ShouldBe( y: 2019, m: 3, d: 8, hr: 2, min: 31, sec: 01, ms: 862, offsetMinutes: 0 );
            ofx.SignOn.Language.Should().Be( "ENG" );
            ofx.SignOn.Institution.Name.Should().Be( "Second Luddite Federal Credit Union" );
            ofx.SignOn.Institution.FId.Should().Be( 9999 );

            ofx.Statements.Count.Should().Be( 7 );

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "1111111111" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "1111111111", BankAccountType.SAVINGS );
                AsertCommonSecondLudditeStatement( stmt, txnCount: 3, ledgerBal: 5572.98M, availableBal: 5567.98M );

                {
                    Transaction txn = stmt.Transactions[0];
                    txn.TransType.Should().Be( OfxTransactionType.CREDIT );
                    txn.Date.Value.ShouldBe( 2019, 2, 28, 0, 0, 0, 0, -8 * 60 );
                    txn.Amount.Should().Be( 0.21M );
                    txn.TransactionId.Should().Be( "201902281" );
                    txn.Name.Should().Be( "Credit Dividend" );
                    txn.Memo.Should().Be( "Credit Dividend" );
                }

                {
                    Transaction txn = stmt.Transactions[1];
                    txn.TransType.Should().Be( OfxTransactionType.CREDIT );
                    txn.Date.Value.ShouldBe( 2019, 2, 2, 0, 0, 0, 0, -8 * 60 );
                    txn.Amount.Should().Be( 5000.00M );
                    txn.TransactionId.Should().Be( "201902022" );
                    txn.Name.Should().Be( "Deposit Transfer From 1212121212" );
                    txn.Memo.Should().Be( "Deposit Transfer From 1212121212" );
                }

                {
                    Transaction txn = stmt.Transactions[2];
                    txn.TransType.Should().Be( OfxTransactionType.CREDIT );
                    txn.Date.Value.ShouldBe( 2019, 1, 31, 0, 0, 0, 0, -8 * 60 );
                    txn.Amount.Should().Be( 0.02M );
                    txn.TransactionId.Should().Be( "201901313" );
                    txn.Name.Should().Be( "Credit Dividend" );
                    txn.Memo.Should().Be( "Credit Dividend" );
                }
            }

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "2222222222" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "2222222222", BankAccountType.CHECKING );
                AsertCommonSecondLudditeStatement( stmt, txnCount: 2, ledgerBal: 5678.74M, availableBal: 5678.74M );

                {
                    Transaction txn = stmt.Transactions[0];
                    txn.TransType.Should().Be( OfxTransactionType.DEBIT );
                    txn.Date.Value.ShouldBe( 2019, 3, 3, 0, 0, 0, 0, -8 * 60 );
                    txn.Amount.Should().Be( -399.92M );
                    txn.TransactionId.Should().Be( "201903034" );
                    txn.Name.Should().Be( "ACH Debit PUGET SOUND ENER BILLP" );
                    txn.Memo.Should().Be( "ACH Debit PUGET SOUND ENER BILLPAY  BILLPAY" ); // NOTE: There is trailing whitespace in the OFX which is trimmed by either SgmlReader or OfxSharp (or both!).
                }

                {
                    Transaction txn = stmt.Transactions[1];
                    txn.TransType.Should().Be( OfxTransactionType.CREDIT );
                    txn.Date.Value.ShouldBe( 2019, 3, 3, 0, 0, 0, 0, -8 * 60 );
                    txn.Amount.Should().Be( 5000.00M );
                    txn.TransactionId.Should().Be( "201903035" );
                    txn.Name.Should().Be( "Descriptive Deposit P2P Transfer" );
                    txn.Memo.Should().Be( "Descriptive Deposit P2P Transfer" );
                }
            }

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "3333333333" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "3333333333", BankAccountType.CHECKING );
                AsertCommonSecondLudditeStatement( stmt, txnCount: 2, ledgerBal: 123.39M, availableBal: 123.39M );
            }

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "4444444444" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "4444444444", BankAccountType.CHECKING );
                AsertCommonSecondLudditeStatement( stmt, txnCount: 3, ledgerBal: 7007.02m, availableBal: 7007.02m );
            }

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "5555555555555555" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "5555555555555555", BankAccountType.CREDITLINE ); // Even though this is a credit-card account with `<ACCTTYPE>CREDITLINE`, the bank's OFX lists it with `<BANKACCTFROM>` instead of `<CCACCTFROM>` so the Account subtype will still be BankAccount and not CreditAccount
                AsertCommonSecondLudditeStatement( stmt, txnCount: 2, ledgerBal: -2276.68M, availableBal: null );

                stmt.Transactions[0].Name.Should().Be( "Ext Credit Card Debit SOYLENT" );
                stmt.Transactions[0].Memo.Should().Be( "Ext Credit Card Debit SOYLENT                  0000000   CA" );
                stmt.Transactions[0].Amount.Should().Be( -37.05M );

                stmt.Transactions[1].Name.Should().Be( "Ext Credit Card Debit SAFEWAY 00" );
                stmt.Transactions[1].Memo.Should().Be( "Ext Credit Card Debit SAFEWAY 0000            REDMOND       WA" );
                stmt.Transactions[1].Amount.Should().Be( -79.44M );
            }

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "6666666666" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "6666666666", BankAccountType.CHECKING );
                AsertCommonSecondLudditeStatement( stmt, txnCount: 3, ledgerBal: 775016.85M, availableBal: 775016.85M );
            }

            {
                OfxStatementResponse stmt = ofx.Statements.Single( st => st.AccountFrom.AccountId == "7777777777" );
                AsertBankAccount( stmt, bankId: "USA", accountId: "7777777777", BankAccountType.SAVINGS );
                AsertCommonSecondLudditeStatement( stmt, txnCount: 0, ledgerBal: 6.03M, availableBal: 0.04M );
            }
        }

        /// <summary>
        /// This test exists because:
        /// <list type="bullet">
        /// <item>As of Q1 2022, Chase bank only offers QFX, not OFX, for transaction downloads - and QFX and OFX are meant to be mutually intelligible.</item>
        /// <item>Chase's QFX files do not conform to the OFX 1.6 spec which handily trips-up most OFX parsers. (Details in subsequent bullets below)</item>
        /// <item>1. There is no empty line between the OFX header and the root <c>&lt;OFX&gt;</c> element's open-tag.</item>
        /// <item>2. There is no <c>&lt;BANKMSGSRSV1&gt;</c> element (instead it has a <c>&lt;CREDITCARDMSGSRSV1&gt;</c> element instead... which also does not conform to OFX 1.6 as that element should be a child of <c>&lt;&gt;</c>).</item>
        /// </list>
        /// </summary>
        [Test]
        public void Should_read_ChaseBankCreditCard_QFX_statements()
        {
            OfxDocument ofx = OfxDocumentReader.FromSgmlFile( filePath: @"Files\chase-credit-card.qfx", ChaseQfxAwareOfxDocumentOptions.Instance );
            ofx.HasSingleStatement( out SingleStatementOfxDocument ofxDocument ).Should().BeTrue();

            ofxDocument.Should().NotBeNull();
            ofxDocument.StatementStart.Should().Be( new DateTimeOffset( 2022, 1, 1, 12, 0, 0, TimeSpan.Zero ) ); // "20220101120000" -> 2022-01-01 12:00:00
            ofxDocument.StatementEnd  .Should().Be( new DateTimeOffset( 2022, 2, 1, 12, 0, 0, TimeSpan.Zero ) ); // "20220201120000" -> 2022-02-01 12:00:00

            Account.CreditAccount acc = ofxDocument.Account.Should().BeOfType<Account.CreditAccount>().Subject; // These files also use `CCACCTFROM` instead of `BANKACCTFROM`, which only has `ACCTID` values, no BANKID nor ACCTTYPE.
            acc.AccountId.Trim().Should().Be("1111880001112222");

            ofxDocument.Transactions.Count.Should().Be(2);
            ofxDocument.Transactions.Sum(x => x.Amount).Should().Be(-21.76M); // 2* -10.88
        }

        private static void AsertCommonSecondLudditeStatement( OfxStatementResponse stmt, Int32 txnCount, Decimal ledgerBal, Decimal? availableBal )
        {
            stmt.OfxTransactionUniqueId.Should().Be( 1 ); // They're all `<TRNUID>1` in the OFX, but is that correct?
            stmt.ResponseStatus.Code.Should().Be( 0 );
            stmt.ResponseStatus.Severity.Should().Be( "INFO" );

            stmt.DefaultCurrency.Should().Be( "USD" );

            stmt.TransactionsStart.ShouldBe( y: 2019, m: 1, d: 1 ); // 20190101
            stmt.TransactionsEnd  .ShouldBe( y: 2019, m: 3, d: 8 ); // 20190308

            stmt.Transactions.Count.Should().Be( txnCount );

            stmt.LedgerBalance.Amount.Should().Be( ledgerBal );
            stmt.LedgerBalance.AsOf.Value.ShouldBe( 2019, 3, 8, 2, 31, 1, 862, offsetMinutes: 0 );

            if( availableBal.HasValue )
            {
                stmt.AvailableBalance.Amount.Should().Be( availableBal );
                stmt.AvailableBalance.AsOf.Value.ShouldBe( 2019, 3, 8, 2, 31, 1, 862, offsetMinutes: 0 );
            }
        }

        private static void AsertBankAccount( OfxStatementResponse stmt, String bankId, String accountId, BankAccountType bankAccountType )
        {
            Account.BankAccount bankAccount = stmt.AccountFrom.Should().BeOfType<Account.BankAccount>().Subject;
            bankAccount.BankId         .Should().Be( bankId );
            bankAccount.BranchId       .Should().BeNull();

            bankAccount.AccountId      .Should().Be( accountId );
            bankAccount.AccountKey     .Should().BeNull();
            bankAccount.AccountType    .Should().Be( AccountType.BANK );
            bankAccount.BankAccountType.Should().Be( bankAccountType );
        }
    }

    public static class MoreAssertions
    {
        public static void ShouldBe( this DateTimeOffset value, Int32 y, Int32 m, Int32 d )
        {
            DateTimeOffset expected = new DateTimeOffset( y, m, d, 0, 0, 0, 0, offset: TimeSpan.Zero );

            value.Should().Be( expected );
        }

        public static void ShouldBe( this DateTimeOffset value, Int32 y, Int32 m, Int32 d, Int32 hr, Int32 min, Int32 sec, Int32 ms, Int32 offsetMinutes )
        {
            DateTimeOffset expected = new DateTimeOffset( y, m, d, hr, min, sec, ms, offset: TimeSpan.FromMinutes( offsetMinutes ) );

            value.Should().Be( expected );
        }
    }
}
