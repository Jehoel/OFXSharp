using System;
using System.Globalization;
using System.Xml;

namespace OfxSharp
{
    /// <summary>LEDGERBAL, AVAILBAL</summary>
    public class Balance
    {
        /// <summary>Returns <see langword="null"/> when <paramref name="elementOrNull"/> is <see langword="null"/>.</summary>
        /// <param name="elementOrNull">Can be <see langword="null"/>.</param>
        /// <param name="culture">Must not be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="culture"/> is <see langword="null"/>.</exception>
        public static Balance FromXmlElementOrNull( XmlNode elementOrNull, CultureInfo culture )
        {
            if( culture is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            if( elementOrNull is null )
            {
                return null;
            }
            else
            {
                XmlElement ledgerbal = elementOrNull.AssertIsElementOneOf( "LEDGERBAL", "AVAILBAL" );

                return new Balance(
                    amount: ledgerbal.RequireSingleElementChildText  ("BALAMT").RequireParseDecimal( culture ),
                    asOf  : ledgerbal.GetSingleElementChildTextOrNull("DTASOF").RequireOptionalParseOfxDateTime()
                );
            }
        }

        public Balance( Decimal amount, DateTimeOffset? asOf )
        {
            this.Amount = amount;
            this.AsOf   = asOf;
        }

        /// <summary><c>&lt;LEDGERBAL&gt;&lt;BALAMT&gt;</c> - Required.</summary>
        public decimal Amount { get; }

        /// <summary><c>&lt;LEDGERBAL&gt;&lt;DTASOF&gt;</c> - Required. (UPDATE: I the OFX 1.6 spec says LEDGERBAL and AVAILBAL's DTASOF value is required, but the other &lt;BAL&gt; element does have an optional DTASOF, I'm unsure if bradesco is really returning &quot;00000000&quot; either intentionally or because they misunderstood the spec - or if the test data was masked to hide PII.</summary>
        public DateTimeOffset? AsOf { get; }
    }
}
