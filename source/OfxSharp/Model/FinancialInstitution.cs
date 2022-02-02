using System;
using System.Xml;

namespace OfxSharp
{
    /// <summary>FI</summary>
    public class FinancialInstitution
    {
        public static FinancialInstitution FromXmlElementOrNull( XmlElement fiOrNull )
        {
            if( fiOrNull is null )
            {
                return null;
            }
            else
            {
                XmlElement fi = fiOrNull.AssertIsElement( "FI", parentElementName: "SONRS" );

                String orgName = fi.RequireSingleElementChildText( "ORG" );
                String fId     = fi.RequireSingleElementChildText( "FID" );

                return new FinancialInstitution( name: orgName, fId: fId );
            }
        }

        /// <param name="name">Can be <see langword="null"/>. Can be empty or whitespace.</param>
        /// <param name="fId">Can be <see langword="null"/>. Can be empty or whitespace.</param>
        public FinancialInstitution( String name, String fId )
        {
            this.Name = name;
            this.FId  = fId;
        }

        /// <summary>Can be <see langword="null"/>.<br />OFX/SIGNONMSGSRSV1/SONRS/FI/ORG</summary>
        public String Name { get; }

        /// <summary>Can be <see langword="null"/>.<br />OFX/SIGNONMSGSRSV1/SONRS/FI/FID<br />&quot;Financial Institution ID (unique within &lt;ORG&gt;), A-32&quot; (i.e. alphanumeric string up to 32 chars in length)</summary>
        public String FId  { get; }
    }
}
