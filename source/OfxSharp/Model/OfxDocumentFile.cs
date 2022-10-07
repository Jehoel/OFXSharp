﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace OfxSharp
{
    /// <summary>Combines <see cref="OfxDocument"/> with <see cref="FileInfo"/>. Implicitly convertible to <see cref="OfxSharp.OfxDocument"/>.</summary>
    public class OfxDocumentFile
    {
        /// <summary>Returns a cached instance of <c>Encoding.GetEncoding( codepage: 1252 )</c>.</summary>
        public static Encoding Windows1252 { get; } = Encoding.GetEncoding( codepage: 1252 );

        /// <param name="ofxFileInfo">Required. Cannot be <see langword="null"/>.</param>
        /// <param name="encoding">Can be <see langword="null"/>. When <see langword="null"/> this defaults to <see cref="Windows1252"/>.</param>
        /// <param name="options">Can be <see langword="null"/>. When <see langword="null"/> this (eventually) defaults to <see cref="DefaultOfxDocumentOptions.Instance"/>.</param>
        /// <param name="cancellationToken">Not currently used. Will be used after <see cref="StreamReader"/> supports it. See https://github.com/dotnet/runtime/issues/20824</param>
        public static async Task<OfxDocumentFile> ReadFileAsync( FileInfo ofxFileInfo, Encoding encoding = null, IOfxReaderOptions options = null, CancellationToken cancellationToken = default )
        {
            if( ofxFileInfo is null ) throw new ArgumentNullException( nameof( ofxFileInfo ) );

            if( encoding is null )
            {
                encoding = Windows1252;
            }

            // HACK: Simpler for now...
            String fileText;
            using( FileStream fs = new FileStream( path: ofxFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 * 1024 * 1024, useAsync: true ) )
            using( StreamReader rdr = new StreamReader( fs, encoding ) )
            {
                fileText = await rdr.ReadToEndAsync(/*cancellationToken*/).ConfigureAwait(false); // Grumble: https://github.com/dotnet/runtime/issues/20824 // "Add CancellationToken to StreamReader.Read* methods"
            }

            using( StringReader fileTextStringReader = new StringReader( fileText ) )
            {
                OfxDocument ofxDoc = OfxDocumentReader.FromSgmlFile( fileTextStringReader, options );
                return new OfxDocumentFile( ofxDoc, ofxFileInfo );
            }
        }

        /// <param name="ofxFileInfo">Required. Cannot be <see langword="null"/>.</param>
        /// <param name="encoding">Can be <see langword="null"/>. When <see langword="null"/> this defaults to <see cref="Windows1252"/>.</param>
        /// <param name="options">Can be <see langword="null"/>. When <see langword="null"/> this (eventually) defaults to <see cref="DefaultOfxDocumentOptions.Instance"/>.</param>
        public static OfxDocumentFile ReadFile( FileInfo ofxFileInfo, Encoding encoding = null, IOfxReaderOptions options = null )
        {
            if( ofxFileInfo is null ) throw new ArgumentNullException( nameof( ofxFileInfo ) );

            if( encoding is null )
            {
                encoding = Windows1252;
            }

            // HACK: Simpler for now...
            String fileText = System.IO.File.ReadAllText( ofxFileInfo.FullName, encoding );

            using( StringReader fileTextStringReader = new StringReader( fileText ) )
            {
                OfxDocument ofxDoc = OfxDocumentReader.FromSgmlFile( fileTextStringReader, options );
                return new OfxDocumentFile( ofxDoc, ofxFileInfo );
            }
        }

        public static implicit operator OfxDocument( OfxDocumentFile self )
        {
            return self.OfxDocument;
        }

        public OfxDocumentFile( OfxDocument ofxDocument, FileInfo file )
        {
            this.OfxDocument = ofxDocument ?? throw new ArgumentNullException( nameof( ofxDocument ) );
            this.File        = file        ?? throw new ArgumentNullException( nameof( file ) );
        }

        public OfxDocument OfxDocument { get; }
        public FileInfo    File        { get; }

        // Not for now...
        /*
        public Byte[] ComputeHash()
        {

        }
        */
    }
}
