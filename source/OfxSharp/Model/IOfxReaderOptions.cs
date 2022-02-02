using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace OfxSharp
{
    /// <summary>Consumers of the OfxSharp library can optionally implement this interface to exercise some control or influence over the OFX parsing and reading process.</summary>
    public interface IOfxReaderOptions
    {
        /// <summary>
        /// If an OFX file contains a <c>&lt;SIGNONMSGSRSV1&gt;&lt;SONRS&gt;&lt;LANGUAGE&gt;</c> value then this method will be invoked with that LANGUAGE value passed in <paramref name="signOnResponseLanguage"/>.<br />
        /// Implementations should return a suitable <see cref="CultureInfo"/> for the specified ISO-639 three-letter code (while also considering the OFX file's specifics and expected bank-specific deviations from the OFX spec, as an OFX could declare its LANGUAGE is French, but still use en-US formatting for numeric values (i.e. using dots for the radix place instead of commas).<br />
        /// Implementations MAY also return <see langword="null"/>, in which case the actual <see cref="CultureInfo"/> used is undefined (the OFX spec doesn't say what should happen if a SONRS omits LANGUAGE, but I think defaulting to either <c>en-US</c> or <see cref="CultureInfo.InvariantCulture"/> - or <see cref="CultureInfo.CurrentCulture"/> is appropriate, as this will likely run in user-interactive scenarios.
        /// </summary>
        /// <param name="signOnResponseLanguage">The argument will be the raw <see cref="String"/> from <see cref="SignOnResponse.Language"/>. The value *SHOULD* be a 3-letter country code from ISO/DIS-3166, e.g. <c>ENG</c> or <c>POR</c>.</param>
        /// <param name="signOnResponse">The parent object that <paramref name="signOnResponseLanguage"/> is obtained from - I concede that passing both <see cref="SignOnResponse"/> and <paramref name="signOnResponseLanguage"/> is kinda redundant, but meh.</param>
        CultureInfo GetCultureFromLanguage( String signOnResponseLanguage, SignOnResponse signOnResponse );

        /// <summary>
        /// Implementations MAY simply return <see langword="null"/>.<br />
        /// Implementations should return <see langword="true"/> if the method parameter arguments provide enough evidence/hints to suggest the OFX file is actually a Chase QFX file which does not conform to OFX 1.6.
        /// Implementations should return <see langword="false"/> if the method parameter arguments provide enough evidence/hints to suggest the OFX file is NOT a Chase QFX file, i.e. the OFX file conforms to OFX 1.6.
        /// </summary>
        /// <param name="header">The parsed OFX colon-separated name+value pairs from the OFX header.</param>
        /// <param name="doc">The SGML OFX file's XML representation.</param>
        Boolean? IsChaseQfx( IReadOnlyDictionary<String,String> header, XmlDocument doc );
    }

    public class DefaultOfxDocumentOptions : IOfxReaderOptions
    {
        /// <summary>Non-singleton convenience instance.</summary>
        public static DefaultOfxDocumentOptions Instance { get; } = new DefaultOfxDocumentOptions();

        public virtual CultureInfo GetCultureFromLanguage( String signOnResponseLanguage, SignOnResponse signOnResponse )
        {
            if( signOnResponseLanguage == "ENG" ) return CultureInfo.InvariantCulture;

            return null;
        }

        public virtual Boolean? IsChaseQfx( IReadOnlyDictionary<String, String> header, XmlDocument doc )
        {
            return null;
        }
    }

    public class ChaseQfxAwareOfxDocumentOptions : DefaultOfxDocumentOptions
    {
        /// <summary>Non-singleton convenience instance.</summary>
        public new static ChaseQfxAwareOfxDocumentOptions Instance { get; } = new ChaseQfxAwareOfxDocumentOptions();

        public override CultureInfo GetCultureFromLanguage( String signOnResponseLanguage, SignOnResponse signOnResponse )
        {
            if( signOnResponseLanguage == "ENG" ) return CultureInfo.InvariantCulture;

            return null;
        }

        public override Boolean? IsChaseQfx( IReadOnlyDictionary<String, String> header, XmlDocument doc )
        {
            Int32 creditResponseCount = doc.GetElementsByTagName( "CREDITCARDMSGSRSV1" ).Count;
            Int32 bankResponseCount   = doc.GetElementsByTagName( "BANKMSGSRSV1"       ).Count;

            if( creditResponseCount == 1 && bankResponseCount == 0 )
            {
                return true;
            }
            else if( creditResponseCount == 0 && bankResponseCount == 1 )
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }
}
