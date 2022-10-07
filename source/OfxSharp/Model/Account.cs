using System;
using System.Xml;

namespace OfxSharp
{
    /// <summary>11.3.1 Banking Account <c>&lt;BANKACCTFROM&gt;</c>, <c>&lt;BANKACCTTO&gt;</c>, <c>&lt;CCACCTFROM&gt;</c></summary>
    public abstract class Account
    {
        public static Account FromXmlElementOrNull( XmlElement accountElementOrNull )
        {
            if( accountElementOrNull is null )
            {
                return null;
            }

            switch( accountElementOrNull.Name )
            {
            case "BANKACCTFROM":
            case "BANKACCTTO":
                return new BankAccount( accountElementOrNull );

            case "CCACCTFROM":
            case "CCACCTTO":
                return new CreditAccount( accountElementOrNull );

            case "INVACCTFROM":
            case "INVACCTTO":
                return new InvestmentAccount( accountElementOrNull );

            case "PRESACCTFROM":
            case "PRESACCTTO":
                // "Bill Presentment Account"
                 return new BillPresentmentAccount( accountElementOrNull );

            default:
                const String MSG_FMT = "Expected null, <BANKACCTFROM>, <BANKACCTTO>, <CCACCTFROM>, <CCACCTTO>, <INVACCTFROM>, <INVACCTTO>, <PRESACCTFROM>, or <PRESACCTTO> but encountered: <{0}>";

                throw new OfxException( message: MSG_FMT.Fmt( accountElementOrNull.Name ) );
            }
        }

        private Account( XmlElement accountElement )//, AccountType type )
        {
            if( accountElement is null ) throw new ArgumentNullException( nameof( accountElement ) );

//          this.AccountType = type;
            this.ElementName = accountElement.Name;
            this.AccountId   = accountElement.GetSingleElementChildTextOrNull( "ACCTID"  );
            this.AccountKey  = accountElement.GetSingleElementChildTextOrNull( "ACCTKEY" );
        }

        public string ElementName { get; }

        /// <summary><c>ACCTID</c>. Can be <see langword="null"/>.</summary>
        public string AccountId   { get; }

        /// <summary><c>ACCTKEY</c> Can be <see langword="null"/>.</summary>
        public string AccountKey  { get; }

//      /// <summary>Varies based on <c>BANKACCTFROM</c> or <c>BANKACCTTO</c> or <c>CCACCTFROM</c></summary>
//      public AccountType AccountType { get; }

        //

        public Boolean IsBankAccount( out BankAccount self )
        {
            self = this as BankAccount;
            return self != null;
        }

        public Boolean IsCreditAccount( out CreditAccount self )
        {
            self = this as CreditAccount;
            return self != null;
        }

        #region Subtypes

        /// <summary><c>&lt;BANKACCTFROM&gt;</c> and <c>&lt;BANKACCTTO&gt;</c></summary>
        public sealed class BankAccount : Account
        {
            public BankAccount( XmlElement bankAccountElement )
                : base( bankAccountElement )
            {
                if( bankAccountElement is null ) throw new ArgumentNullException( nameof( bankAccountElement ) );

                this.BankId               = bankAccountElement.GetSingleElementChildTextOrNull( "BANKID"   );
                this.BranchId             = bankAccountElement.GetSingleElementChildTextOrNull( "BRANCHID" );
                this.BankAccountTypeValue = bankAccountElement.RequireSingleElementChildText  ( "ACCTTYPE" );
                this.BankAccountType      = this.BankAccountTypeValue.ParseEnum<BankAccountType>();

                this.ExtBankAccountTo     = bankAccountElement.GetSingleElementChildOrNull( "EXTBANKACCTTO" );
            }

            /// <summary>BANKID</summary>
            public string BankId { get; }

            /// <summary>BRANCHID</summary>
            public string BranchId { get; }

            // ACCTID inherited

            /// <summary>ACCTTYPE / ACCTTYPE2</summary>
            public BankAccountType BankAccountType { get; }

            /// <summary>ACCTTYPE (Raw value)</summary>
            public String BankAccountTypeValue { get; }

            //

            /// <summary><c>&lt;EXTBANKACCTTO&gt;?</c><br />With child elements (SGML content): <c>(BANKNAME?, BANKBRANCH?, BANKCITY?, BANKPOSTALCODE?, CHE.PTTACCTID?)</c></summary>
            public XmlElement ExtBankAccountTo { get; }
        }

        /// <summary><c>&lt;CCACCTFROM&gt;</c> and <c>&lt;CCACCTTO&gt;</c></summary>
        public sealed class CreditAccount : Account
        {
            public CreditAccount( XmlElement creditAccountElement )
                : base( creditAccountElement )
            {
                if( creditAccountElement is null ) throw new ArgumentNullException( nameof( creditAccountElement ) );
            }

            // ACCTID  inherited
            // ACCTKEY inherited
        }

        /// <summary><c>&lt;INVACCTFROM&gt;</c> and <c>&lt;INVACCTTO&gt;</c></summary>
        public sealed class InvestmentAccount : Account
        {
            public InvestmentAccount( XmlElement invAccountElement )
                : base( invAccountElement )
            {
                if( invAccountElement is null ) throw new ArgumentNullException( nameof(invAccountElement) );

                //

                this.BrokerId = invAccountElement.GetSingleElementChildTextOrNull( "BROKERID" );
            }

            // ACCTID inherited

            /// <summary>BROKERID</summary>
            public string BrokerId { get; }
        }

        /// <summary><c>&lt;PRESACCTFROM&gt;</c> and <c>&lt;PRESACCTTO&gt;</c></summary>
        public sealed class BillPresentmentAccount : Account
        {
            public BillPresentmentAccount( XmlElement invAccountElement )
                : base( invAccountElement )
            {
                if( invAccountElement is null ) throw new ArgumentNullException( nameof(invAccountElement) );

                //

                this.BILLPUB         = invAccountElement.GetSingleElementChildTextOrNull( "BILLPUB" );
                this.BILLERID        = invAccountElement.GetSingleElementChildTextOrNull( "BILLERID" );
                this.BILLERNAME      = invAccountElement.GetSingleElementChildTextOrNull( "BILLERNAME" );
                this.PRESNAMEADDRESS = invAccountElement.GetSingleElementChildTextOrNull( "PRESNAMEADDRESS" );
                this.USERID          = invAccountElement.GetSingleElementChildTextOrNull( "USERID" );
            }

            // ACCTID inherited

            /// <summary>BILLPUB</summary>
            public string BILLPUB { get; }

            /// <summary>BILLERID</summary>
            public string BILLERID { get; }

            /// <summary>BILLERNAME</summary>
            public string BILLERNAME { get; }

            /// <summary>PRESNAMEADDRESS</summary>
            public string PRESNAMEADDRESS { get; }

            /// <summary>USERID</summary>
            public string USERID { get; }
        }

        #endregion
    }
}
