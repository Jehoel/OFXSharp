using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Sgml;

namespace OfxSharp
{
    public static class OfxDocumentReader
    {
        private enum State
        {
            BeforeOfxHeader,
            InOfxHeader,
            StartOfOfxSgml
        }

        #region Non-async

        public static OfxDocument FromSgmlFile( FileInfo file ) => FromSgmlFile( file: file, options: null );

        public static OfxDocument FromSgmlFile( FileInfo file, IOfxReaderOptions? options )
        {
            if( file is null ) throw new ArgumentNullException( nameof( file ) );

            return FromSgmlFile( filePath: file.FullName, options );
        }

        //

        public static OfxDocument FromSgmlFile( String filePath ) => FromSgmlFile( filePath: filePath, options: null );

        public static OfxDocument FromSgmlFile( String filePath, IOfxReaderOptions? options )
        {
            if( filePath is null ) throw new ArgumentNullException( nameof( filePath ) );

            using( FileStream fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan ) )
            {
                return FromSgmlFile( fs, options );
            }
        }

        //

        public static OfxDocument FromSgmlFile( Stream stream ) => FromSgmlFile( stream: stream, options: null );

        public static OfxDocument FromSgmlFile( Stream stream, IOfxReaderOptions? options )
        {
            if( stream is null ) throw new ArgumentNullException( nameof( stream ) );

            using( StreamReader rdr = new StreamReader( stream ) )
            {
                return FromSgmlFile( reader: rdr, options );
            }
        }

        //

        /// <summary>This method uses <see cref="DefaultOfxDocumentOptions.Instance"/> to see if <paramref name="reader"/> is for a Chase bastardized QFX file, in which case <see cref="FromChaseQfxXmlElement"/> is used, otherwise <see cref="FromOfxXmlElement"/> is used.</summary>
        public static OfxDocument FromSgmlFile( TextReader reader ) => FromSgmlFile( reader: reader, options: null );

        /// <summary>This method uses <paramref name="optionsOrNull"/> (if null, then <see cref="DefaultOfxDocumentOptions.Instance"/> is used) to see if <paramref name="reader"/> is for a Chase bastardized QFX file, in which case <see cref="FromChaseQfxXmlElement"/> is used, otherwise <see cref="FromOfxXmlElement"/> is used.</summary>
        public static OfxDocument FromSgmlFile( TextReader reader, IOfxReaderOptions? options )
        {
            if( reader is null ) throw new ArgumentNullException( nameof( reader ) );

            options ??= new DefaultOfxDocumentOptions();

            // Read the header:
            IReadOnlyDictionary<String,String> header = ReadOfxFileHeaderUntilStartOfSgml( reader );

            XmlDocument doc = ConvertSgmlToXml( reader );

#if DEBUG
            String xmlDocString = doc.ToXmlString();
#endif

            Boolean? isChaseQfx = options.IsChaseQfx( header, doc );
            if( isChaseQfx.HasValue && isChaseQfx.Value )
            {
                return OfxDocument.FromChaseQfxXmlElement( doc.DocumentElement, options );
            }
            else
            {
                return OfxDocument.FromOfxXmlElement( doc.DocumentElement, options );
            }
        }

        /// <summary>This method assumes there is always a blank-line between the OFX header (the colon-bifurcated lines) and the <c>&gt;OFX&lt;</c> line.</summary>
        private static IReadOnlyDictionary<String,String> ReadOfxFileHeaderUntilStartOfSgml( TextReader reader )
        {
            Dictionary<String,String> sgmlHeaderValues = new Dictionary<String,String>();

            //

            State state = State.BeforeOfxHeader;
            String? line;

            while( ( line = reader.ReadLine() ) != null )
            {
                switch( state )
                {
                case State.BeforeOfxHeader:
                    if( line.IsSet() )
                    {
                        state = State.InOfxHeader;
                    }
                    break;

                case State.InOfxHeader:

                    if( line.IsEmpty() )
                    {
                        return sgmlHeaderValues;
                    }
                    else if( line.IndexOf( ':' ) > -1 )
                    {
                        // `line` should be either empty/whitespace, or a 2-tuple separated by a single colon, like this: `OLDFILEUID:NONE`.
                        // Anything else is invalid so throw.

                        String[] parts = line.Split( ':' );
                        if( parts.Length == 2 )
                        {
                            String name  = parts[0];
                            String value = parts[1];
                            sgmlHeaderValues.Add( name, value );
                        }
                        else
                        {
                            throw new FormatException( message: "Expected OFX Header line to consist of a single colon bifurcating two non-empty strings, but encountered : \"" + line + "\" instead." );
                        }
                    }
                    else
                    {
                        throw new FormatException( message: "Expected OFX Header line to contain a colon character, but encountered : \"" + line + "\" instead." );
                    }

                    break;

                case State.StartOfOfxSgml:
                default:
                    throw new InvalidOperationException( "This state should never be entered." );
                }

                // HACK: Sometimes a QFX/OFX file (namely from Chase Bnka) won't have a blank-line separator between the OFX Header and the opening <OFX> tag - so use Peek() to check for `<`.
                {
                    Int32 nextChar = reader.Peek();
                    if( nextChar == '<' )
                    {
                        return sgmlHeaderValues;
                    }
                }

            }

            throw new InvalidOperationException( "Reached end of OFX file without encountering end of OFX header." );
        }

        private static XmlDocument ConvertSgmlToXml( TextReader reader )
        {
            // Convert SGML to XML:
            try
            {
                SgmlDtd ofxSgmlDtd = ReadOfxSgmlDtd();

                SgmlReader sgmlReader = new SgmlReader();
                sgmlReader.WhitespaceHandling = WhitespaceHandling.None; // hmm, this doesn't work.
                // Hopefully the next update to `` will include my changes to support trimmed output: https://github.com/lovettchris/SgmlReader/issues/15
                sgmlReader.InputStream        = reader;
                sgmlReader.DocType            = "OFX"; // <-- This causes DTD magic to happen. I don't know where it gets the DTD from though.
                sgmlReader.Dtd                = ofxSgmlDtd;

                // https://stackoverflow.com/questions/1346995/how-to-create-a-xmldocument-using-xmlwriter-in-net
                XmlDocument doc = new XmlDocument();
                using( XmlWriter xmlWriter = doc.CreateNavigator().AppendChild() )
                {
                    while( !sgmlReader.EOF )
                    {
                        xmlWriter.WriteNode( sgmlReader, defattr: true );
                    }
                }

                return doc;
            }
#pragma warning disable IDE0059 // Unnecessary assignment of a value // The `ex` variable exists to assist debugging.
#pragma warning disable CS0168 // Variable is declared but never used
            catch( Exception ex )
#pragma warning restore
            {
                throw;
            }
        }

        private static SgmlDtd ReadOfxSgmlDtd()
        {
            // Need to strip the DTD envelope, apparently...:  https://github.com/lovettchris/SgmlReader/issues/13#issuecomment-862666405
            String dtdText;
            using( FileStream fs = new FileStream( @"C:\git\forks\OFXSharp\source\Specifications\OFX1.6\ofx160.trimmed.dtd", FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096 ) )
            using( StreamReader rdr = new StreamReader( fs ) )
            {
                dtdText = rdr.ReadToEnd();
            }

            // Example cribbed from https://github.com/lovettchris/SgmlReader/blob/363decf083dd847d18c4c765cf0b87598ca491a0/SgmlTests/Tests-Logic.cs

            using( StringReader dtdReader = new StringReader( dtdText ) )
            {
                SgmlDtd dtd = SgmlDtd.Parse(
                    baseUri : null,
                    name    : "OFX",
                    input   : dtdReader,
                    subset  : "",
                    nt      : new NameTable(),
                    resolver: new DesktopEntityResolver()
                );

                return dtd;
            }
        }

        #endregion

        #region Async

        public static async Task<OfxDocument> FromSgmlFileAsync( Stream stream, CancellationToken cancellationToken = default )
        {
            using( StreamReader rdr = new StreamReader( stream, detectEncodingFromByteOrderMarks: true ) )
            {
                return await FromSgmlFileAsync( reader: rdr, cancellationToken ).ConfigureAwait(false);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter // `cancellationToken`
        public static async Task<OfxDocument> FromSgmlFileAsync( TextReader reader, CancellationToken cancellationToken = default )
#pragma warning restore IDE0060
        {
            if( reader is null ) throw new ArgumentNullException( nameof( reader ) );

            // HACK: Honestly, it's easier just to buffer it all first:

            String text = await reader.ReadToEndAsync(/*cancellationToken*/).ConfigureAwait(false);

            using( StringReader sr = new StringReader( text ) )
            {
                return FromSgmlFile( sr );
            }
        }

        #endregion
    }
}
