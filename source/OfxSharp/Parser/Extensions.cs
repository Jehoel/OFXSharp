using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace OfxSharp
{
	internal static class Extensions
	{
		public static string Fmt(this string format, params object[] args)
		{
			return String.Format( CultureInfo.CurrentCulture, format, args );
		}

		public static string FmtInv(this string format, params object[] args)
		{
			return String.Format( CultureInfo.InvariantCulture, format, args );
		}

        public static bool IsSet(this string s)
		{
			return !String.IsNullOrWhiteSpace(s);
		}

        public static bool IsEmpty(this string s)
		{
			return String.IsNullOrWhiteSpace(s);
		}

        public static TEnum ParseEnum<TEnum>(this string s)
           where TEnum : struct, Enum
        {
            if( Enum.TryParse<TEnum>( s, ignoreCase: true, out TEnum value ) )
            {
                return value;
            }
            else
            {
                throw new FormatException( "Couldn't parse \"{0}\" as a {1} enum value.".Fmt( s, typeof(TEnum).Name ) );
            }
        }

        public static TEnum? TryParseEnum<TEnum>(this string s)
           where TEnum : struct, Enum
        {
            if( Enum.TryParse<TEnum>( s, ignoreCase: true, out TEnum value ) )
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public static TEnum? MaybeParseEnum<TEnum>(this string s)
            where TEnum : struct, Enum
        {
            return Enum.TryParse<TEnum>( s, ignoreCase: true, out TEnum value ) ? value : (TEnum?)null;
        }

        /// <summary>This method always uses <see cref="CultureInfo.InvariantCulture"/>.</summary>
        public static Int32 RequireParseInt32Inv(this string s)
        {
            if( s is null ) throw new ArgumentNullException( nameof( s ) );

            //

            return Int32.Parse( s, NumberStyles.Integer, CultureInfo.InvariantCulture );
        }

#if ORIGINAL_BUT_INFLEXIBLE
        public static Decimal RequireParseDecimal(this string s, CultureInfo culture)
        {
            if( s       is null ) throw new ArgumentNullException( nameof( s ) );
            if( culture is null ) throw new ArgumentNullException( nameof( culture ) );

            //

            return Decimal.Parse( s, NumberStyles.Any, culture );
        }
#else
        public static Decimal RequireParseOfxDecimal(this string s)
        {
            const String MSG_FMT = "Cannot parse \"{0}\" as a decimal. The value appears to contain multiple radix point characters (recognized characters: `.`, `,`, `'`, and `٫`.";

            if( s is null ) throw new ArgumentNullException( nameof( s ) );

            // A problem is that two separate OFX files will have `<LANGUAGE>POR`, but one file (bradesco.ofx) will use commas for the decimal point, but another file (itau.ofx) will use a dot. ARGH.
            // It turns out we don't need to pass around CultureInfo for decimal values: According to Wikipedia, *the entire world* only uses 1 of 4 possible chars as decimal separators:
            // https://en.wikipedia.org/wiki/File:DecimalSeparator.svg
            // 1. '.' - dot / period / full-stop. en-US, en-GB, etc.
            // 2. ',' - comma                   . fr-FR, pt-BR, etc.
            // 3. ''' - apostrophe              . Canada, apparently.
            // 4. '٫' - momayyez                . Most middle-eastern countries.

            //

            Int32 dot0Idx        = s.IndexOf    ( '.' );
            Int32 dotNIdx        = s.LastIndexOf( '.' );

            Int32 comma0Idx      = s.IndexOf    ( ',' );
            Int32 commaNIdx      = s.LastIndexOf( ',' );

            Int32 apostrophe0Idx = s.IndexOf    ( '\'' );
            Int32 apostropheNIdx = s.LastIndexOf( '\'' );

            Int32 momayyez0Idx   = s.IndexOf    ( '٫' );
            Int32 momayyezNIdx   = s.LastIndexOf( '٫' );

            // We expect that:
            // * Siblings in an idx-pairs should be equal to each other (i.e. `dot0Idx == dotNIdx`, `comma0Idx == commaNIdx`), because there should never be more than 1 radix point in a numeric value.
            // * 3 of the pairs should all be -1 and only 1 pair should have a non-negative value.
            // * I will allow all 4 pairs to be -1, such as when a number is correctly rendered as an integer, e.g. `<VALUE>123` - but this might be incorrect behaviour, so this could change in future.

            Boolean allPairsSiblingsMatch = (
                ( dot0Idx        == dotNIdx        ) &&
                ( comma0Idx      == commaNIdx      ) &&
                ( apostrophe0Idx == apostropheNIdx ) &&
                ( momayyez0Idx   == momayyezNIdx   )
            );

            if( !allPairsSiblingsMatch )
            {
                throw new FormatException( MSG_FMT.FmtInv( s ) );
            }

            //

            if( dot0Idx >= 0 )
            {
                if( /*dot0Idx >= 0 ||*/ comma0Idx >= 0 || apostrophe0Idx >= 0 || momayyez0Idx >= 0 ) throw new FormatException( MSG_FMT.FmtInv( s ) );

                return Decimal.Parse( s, NumberStyles.Any, _enUS_for_dot );
            }
            else if( comma0Idx >= 0 )
            {
                if( dot0Idx >= 0 || /*apostrophe0Idx >= 0 ||*/ momayyez0Idx >= 0 ) throw new FormatException( MSG_FMT.FmtInv( s ) );

                return Decimal.Parse( s, NumberStyles.Any, _ptBR_for_com );
            }
            else if( apostrophe0Idx >= 0 )
            {
                if( dot0Idx >= 0 || comma0Idx >= 0 || /*apostrophe0Idx >= 0 ||*/ momayyez0Idx >= 0 ) throw new FormatException( MSG_FMT.FmtInv( s ) );

                throw new NotImplementedException( "Support for apostrophes as decimal-point chars is not yet implemented." );
            }
            else if( momayyez0Idx >= 0 )
            {
                if( dot0Idx >= 0 || comma0Idx >= 0 || /*apostrophe0Idx >= 0 ||*/ momayyez0Idx >= 0 ) throw new FormatException( MSG_FMT.FmtInv( s ) );

                throw new NotImplementedException( "Support for momayyez as decimal-point chars is not yet implemented." );
            }
            else // i.e. there are zero radix point chars.
            {
                return Decimal.Parse( s, NumberStyles.Any, CultureInfo.InvariantCulture );
            }
        }

        private static readonly CultureInfo _enUS_for_dot = Cultures.ENUS;
        private static readonly CultureInfo _ptBR_for_com = Cultures.PTBR;
//      private static readonly CultureInfo _xxxx_for_apo = null;// Cultures.PTBR; // is it *really* Canada tho?
//      private static readonly CultureInfo _xxxx_for_mom = null;// Cultures.PTBR;

#endif

        private static readonly XmlWriterSettings _xmlSettings = new XmlWriterSettings()
        {
            NewLineHandling = NewLineHandling.Replace,
            NewLineChars    = "\r\n",
            Indent          = true,
            IndentChars     = "\t",
            ConformanceLevel = ConformanceLevel.Document,
        };

        public static String ToXmlString( this XmlDocument doc )
        {
            String xmlString;

            using( StringWriter stringWriter = new StringWriter() )
            using( XmlWriter xmlTextWriter = XmlWriter.Create( stringWriter, _xmlSettings ) )
            {
                doc.WriteTo( xmlTextWriter );

                xmlTextWriter.Flush();

                xmlString = stringWriter.ToString();
            }

            return xmlString;
        }
	}
}
