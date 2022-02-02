using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace OfxSharp
{
    public class OfxDocument
    {
        #region Factories

        /// <param name="ofxElement">Required. Cannot be <see langword="null"/>. Must be an <c>&lt;OFX&gt;</c> element.</param>
        /// <param name="options">Required. Cannot be <see langword="null"/>.</param>
        public static OfxDocument FromOfxXmlElement( XmlElement ofxElement, IOfxReaderOptions options )
        {
            if( ofxElement is null ) throw new ArgumentNullException( nameof( ofxElement ) );
            if( options    is null ) throw new ArgumentNullException( nameof( options ) );

            //

            _ = ofxElement.AssertIsElement( "OFX" );

            XmlElement signOnMessageResponse = ofxElement.RequireSingleElementChild("SIGNONMSGSRSV1");
            XmlElement bankMessageResponse   = ofxElement.RequireSingleElementChild("BANKMSGSRSV1");

            SignOnResponse sonrs = SignOnResponse.FromXmlElement( signOnMessageResponse );

            CultureInfo ofxFileCulture = options.GetCulture( sonrs );
            if( ofxFileCulture is null ) throw new InvalidOperationException( "The "+ nameof(IOfxReaderOptions) + "." + nameof(IOfxReaderOptions.GetCulture) + " call returned null. Implementations must not return null." );

            return new OfxDocument(
                signOn    : sonrs,
                statements: GetStatements( bankMessageResponse, ofxFileCulture ),
                culture   : ofxFileCulture
            );
        }

        /// <param name="bankMessageResponse">Must be a <c>&lt;BANKMSGSRSV1&gt;</c> element.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static IEnumerable<OfxStatementResponse> GetStatements( XmlElement bankMessageResponse, CultureInfo culture )
        {
            if( bankMessageResponse is null ) throw new ArgumentNullException( nameof( bankMessageResponse ) );
            if( culture             is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            _ = bankMessageResponse.AssertIsElement( "BANKMSGSRSV1" );

            foreach( XmlElement stmTrnResponse in bankMessageResponse.GetChildNodes("STMTTRNRS") )
            {
                XmlElement stmtTrnRs = stmTrnResponse.AssertIsElement("STMTTRNRS");

                yield return OfxStatementResponse.FromSTMTTRNRS( stmtTrnRs, culture );
            }
        }

        /// <param name="chaseQfxElement">Despite the reference to &quot;QFX&quot; the element's tag-name is still &quot;&lt;OFX&gt;&quot;.</param>
        public static OfxDocument FromChaseQfxXmlElement( XmlElement chaseQfxElement, IOfxReaderOptions options )
        {
            if( chaseQfxElement is null ) throw new ArgumentNullException( nameof(chaseQfxElement) );
            if( options         is null ) throw new ArgumentNullException( nameof( options ) );

            //

            _ = chaseQfxElement.AssertIsElement( "OFX" );

            XmlElement signOnMessageResponse     = chaseQfxElement.RequireSingleElementChild("SIGNONMSGSRSV1");
            XmlElement creditCardMessageResponse = chaseQfxElement.RequireSingleElementChild("CREDITCARDMSGSRSV1");

            SignOnResponse sonrs = SignOnResponse.FromXmlElement( signOnMessageResponse );

            CultureInfo ofxFileCulture = options.GetCulture( sonrs );
            if( ofxFileCulture is null ) throw new InvalidOperationException( "The "+ nameof(IOfxReaderOptions) + "." + nameof(IOfxReaderOptions.GetCulture) + " call returned null. Implementations must not return null." );

            return new OfxDocument(
                signOn    : SignOnResponse.FromXmlElement( signOnMessageResponse ),
                statements: GetChaseQfxStatements( creditCardMessageResponse, ofxFileCulture ),
                culture   : ofxFileCulture
            );
        }

        /// <param name="creditCardMessageResponse">Must be a <c>&lt;CREDITCARDMSGSRSV1&gt;</c> element.</param>
        /// <param name="culture">Required. Cannot be <see langword="null"/>.</param>
        public static IEnumerable<OfxStatementResponse> GetChaseQfxStatements( XmlElement creditCardMessageResponse, CultureInfo culture )
        {
            if( creditCardMessageResponse is null ) throw new ArgumentNullException( nameof(creditCardMessageResponse) );
            if( culture                   is null ) throw new ArgumentNullException( nameof(culture) );

            //

            _ = creditCardMessageResponse.AssertIsElement( "CREDITCARDMSGSRSV1" );

            foreach( XmlElement stmTrnResponse in creditCardMessageResponse.GetChildNodes("CCSTMTTRNRS") )
            {
                XmlElement stmtTrnRs = stmTrnResponse.AssertIsElement("CCSTMTTRNRS");

                yield return OfxStatementResponse.FromCCSTMTTRNRS( stmtTrnRs, culture );
            }
        }

        #endregion

        /// <summary></summary>
        /// <param name="signOn"></param>
        /// <param name="statements"></param>
        /// <param name="culture">The <see cref="CultureInfo"/> used to parse values in the serialized OFX SGML - or if this is an in-memory <see cref="OfxDocument"/> not being constructed from a file, then it's whatever culture the consuming code wants to associate with this <see cref="OfxDocument"/>. Exposed via <see cref="Culture"/>.</param>
        public OfxDocument( SignOnResponse signOn, IEnumerable<OfxStatementResponse> statements, CultureInfo culture )
        {
            if( signOn     is null ) throw new ArgumentNullException( nameof( signOn ) );
            if( statements is null ) throw new ArgumentNullException( nameof( statements ) );
            if( culture    is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            this.SignOn  = signOn;
            this.Culture = culture;

            this.Statements.AddRange( statements );
        }

        /// <summary><c>SIGNONMSGSRSV1</c>. Required. Cannot be <see langword="null"/>.</summary>
        public SignOnResponse SignOn { get; }

        /// <summary>Never <see langword="null"/>. The <see cref="CultureInfo"/> associated with this <see cref="OfxDocument"/>, e.g. it's the source of the <see cref="NumberFormatInfo"/> used to parse monetary amount values.</summary>
        public CultureInfo Culture { get; }

        /// <summary><c>BANKMSGSRSV1/STMTTRNRS</c> (or <c>CREDITCARDMSGSRSV1/CCSTMTTRNRS</c> for Chase QFX)</summary>
        /// <remarks>Why is this a mutable list?</remarks>
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
