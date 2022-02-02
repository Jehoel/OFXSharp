using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace OfxSharp
{
    public class OfxDocument
    {
        #region Factories

        public static OfxDocument FromOfxXmlElement( XmlElement ofxElement )
        {
            _ = ofxElement.AssertIsElement( "OFX" );

            XmlElement signOnMessageResponse = ofxElement.RequireSingleElementChild("SIGNONMSGSRSV1");
            XmlElement bankMessageResponse   = ofxElement.RequireSingleElementChild("BANKMSGSRSV1");

            return new OfxDocument(
                signOn    : SignOnResponse.FromXmlElement( signOnMessageResponse ),
                statements: GetStatements( bankMessageResponse )
            );
        }

        /// <param name="bankMessageResponse">Must be a <c>&lt;BANKMSGSRSV1&gt;</c> element.</param>
        public static IEnumerable<OfxStatementResponse> GetStatements( XmlElement bankMessageResponse )
        {
            _ = bankMessageResponse.AssertIsElement( "BANKMSGSRSV1" );

            foreach( XmlElement stmTrnResponse in bankMessageResponse.GetChildNodes("STMTTRNRS") )
            {
                XmlElement stmtTrnRs = stmTrnResponse.AssertIsElement("STMTTRNRS");

                yield return OfxStatementResponse.FromSTMTTRNRS( stmtTrnRs );
            }
        }

        /// <param name="chaseQfxElement">Despite the reference to &quot;QFX&quot; the element's tag-name is still &quot;&lt;OFX&gt;&quot;.</param>
        public static OfxDocument FromChaseQfxXmlElement( XmlElement chaseQfxElement )
        {
            if( chaseQfxElement is null ) throw new ArgumentNullException( nameof(chaseQfxElement) );

            _ = chaseQfxElement.AssertIsElement( "OFX" );

            XmlElement signOnMessageResponse     = chaseQfxElement.RequireSingleElementChild("SIGNONMSGSRSV1");
            XmlElement creditCardMessageResponse = chaseQfxElement.RequireSingleElementChild("CREDITCARDMSGSRSV1");

            return new OfxDocument(
                signOn    : SignOnResponse.FromXmlElement( signOnMessageResponse ),
                statements: GetChaseQfxStatements( creditCardMessageResponse )
            );
        }

        /// <param name="creditCardMessageResponse">Must be a <c>&lt;CREDITCARDMSGSRSV1&gt;</c> element.</param>
        public static IEnumerable<OfxStatementResponse> GetChaseQfxStatements( XmlElement creditCardMessageResponse )
        {
            if( creditCardMessageResponse is null ) throw new ArgumentNullException( nameof(creditCardMessageResponse) );

            _ = creditCardMessageResponse.AssertIsElement( "CREDITCARDMSGSRSV1" );

            foreach( XmlElement stmTrnResponse in creditCardMessageResponse.GetChildNodes("CCSTMTTRNRS") )
            {
                XmlElement stmtTrnRs = stmTrnResponse.AssertIsElement("CCSTMTTRNRS");

                yield return OfxStatementResponse.FromCCSTMTTRNRS( stmtTrnRs );
            }
        }

        #endregion

        public OfxDocument( SignOnResponse signOn, IEnumerable<OfxStatementResponse> statements )
        {
            this.SignOn = signOn ?? throw new ArgumentNullException( nameof( signOn ) );

            this.Statements.AddRange( statements );
        }

        /// <summary><c>SIGNONMSGSRSV1</c>. Required. Cannot be <see langword="null"/>.</summary>
        public SignOnResponse SignOn { get; }

        /// <summary><c>BANKMSGSRSV1/STMTTRNRS</c> (or <c>CREDITCARDMSGSRSV1/CCSTMTTRNRS</c> for Chase QFX)</summary>
        public List<OfxStatementResponse> Statements { get; } = new List<OfxStatementResponse>();

        //

        /// <summary>Utility method to allow library consumers to more easily handle the simpler and more common case of single-statement OFX files. This method returns false if this <see cref="OfxDocument"/> is a multi-statement OFX file.</summary>
        public Boolean HasSingleStatement( out SingleStatementOfxDocument doc )
        {
            if( this.Statements.Count == 1 )
            {
                doc = new SingleStatementOfxDocument( this );
                return true;
            }
            else
            {
                doc = default;
                return false;
            }
        }
    }
}
