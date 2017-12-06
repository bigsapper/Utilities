using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;

using Framework;

namespace Framework
{
	/// <summary>
	/// Summary description for Utilities.
	/// </summary>
	// changed to a static class
	public static class Utilities
	{
		public const string BAD_CHARS_NULL = @".&'#@";
		public const string BAD_CHARS_SPACE = @"`\/";

		public static string ApplicationVersionInfos(string Function)
		{
			AssemblyName assy = Assembly.GetExecutingAssembly().GetName();

			return "NET " + Function + " Utilities " + assy.Version;
		}

		// added support for sending HTML messages
		// fix bug with multiple email addresses
		public static void SendEmail(string FromAddress, string FromName, string ToAddress, string ToName, string Subject, string Message, bool IsHtml)
		{
			MailMessage mail = new MailMessage();
			mail.Subject = Subject;
			mail.Body = Message;
			mail.IsBodyHtml = IsHtml;

			mail.From = new MailAddress(FromAddress, FromName);

			string[] toAddresses = ToAddress.Split(',');
			for ( int i = 0; i < toAddresses.Length; i++ )
			{
				MailAddress to = new MailAddress(toAddresses[i].Trim());
				mail.To.Add(to);
			}

			SmtpClient smtp = new SmtpClient("localhost");
			smtp.Send(mail);

			return;
		}

		#region Misc. XML Functions

		// filters control characters but allows only properly-formed surrogate sequences
		private static Regex _invalidXMLChars = new Regex(
			@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
			RegexOptions.Compiled);

		/// <summary>
		/// removes any unusual unicode characters that can't be encoded into XML
		/// </summary>
		public static string RemoveInvalidXMLChars(string text)
		{
			// added for bug fix
			if ( text.IsNullOrEmpty() ) return "";
			return _invalidXMLChars.Replace(text, "");
		}

		#endregion

		#region DBValueNullCheck()
		// added DBValueNullCheck methods

		public static void DBValueNullCheck(object DBValue, ref int ReturnValue)
		{
			const int DEFAULT_VALUE = 0;

			try
			{
				if ( DBValue != DBNull.Value && DBValue != null )
				{
					ReturnValue = Convert.ToInt32(DBValue);
				}
				else
				{
					ReturnValue = DEFAULT_VALUE;
				}
			}
			catch ( Exception )
			{
				ReturnValue = DEFAULT_VALUE;
			}
		}

		public static void DBValueNullCheck(object DBValue, ref double ReturnValue)
		{
			const double DEFAULT_VALUE = 0;

			try
			{
				if ( DBValue != DBNull.Value && DBValue != null )
				{
					ReturnValue = Convert.ToDouble(DBValue);
				}
				else
				{
					ReturnValue = DEFAULT_VALUE;
				}
			}
			catch ( Exception )
			{
				ReturnValue = DEFAULT_VALUE;
			}
		}

		public static void DBValueNullCheck(object DBValue, ref string ReturnValue)
		{
			const string DEFAULT_VALUE = "";

			try
			{
				if ( DBValue != DBNull.Value && DBValue != null )
				{
					ReturnValue = Convert.ToString(DBValue);
				}
				else
				{
					ReturnValue = DEFAULT_VALUE;
				}
			}
			catch ( Exception )
			{
				ReturnValue = DEFAULT_VALUE;
			}
		}

		public static void DBValueNullCheck(object DBValue, ref bool ReturnValue)
		{
			const bool DEFAULT_VALUE = false;

			try
			{
				if ( DBValue != DBNull.Value && DBValue != null )
				{
					ReturnValue = Convert.ToBoolean(DBValue);
				}
				else
				{
					ReturnValue = DEFAULT_VALUE;
				}
			}
			catch ( Exception )
			{
				ReturnValue = DEFAULT_VALUE;
			}
		}

		#endregion

		#region ValidateAndFormatSSN()

		public static string ValidateAndFormatSSN(object SSN)
		{
			return ValidateAndFormatSSN(SSN, null);
		}

		public static string ValidateAndFormatSSN(object SSN, ArrayList BadSSNs)
		{
			// TODO: SSA revised rules for SSNs effective 6/25/2011; "Randomization"
			//		area   - Any number but 000, 666, 900-999
			//		group  - Any number but 00
			//		serial - Any number but 0000

			// handle null cases first
			string retval = SSN.FormatString();

			// remove any/all non-numeriuc characters
			retval = Utilities.CleanNumeric(retval);

			// validate
			if ( retval.IsNumeric() )
			{
				// Format SSN w/pre-pended zeros
				retval = retval.PadLeft(9, '0');

				// now that we have a numeric valued SSN that's 9-digits long,
				// let's apply some more sophisticated validations...
				int area = Convert.ToInt32(retval.Substring(0, 3));
				int group = Convert.ToInt32(retval.Substring(3, 2));
				int serial = Convert.ToInt32(retval.Substring(5, 4));

				if ( area == 0 || area == 666 || ( area >= 734 && area <= 749 ) || area > 772 )
				{
					retval = "";
				}
				else if ( group == 0 )
				{
					retval = "";
				}
				else if ( serial == 0 )
				{
					retval = "";
				}
				else
				{
					if ( BadSSNs != null )
					{
						if ( BadSSNs.Contains(retval) )
						{
							retval = "";
						}
					}
				}
			}
			else
			{
				retval = "";
			}

			return retval;
		}

		#endregion

		#region DataSet XML Serialization

		public static DataSet DataSetXmlDeserialize(string xml)
		{
			StringReader rdr = new StringReader(xml);

			DataSet ds = new DataSet();
			// read schema so empty elements will pass thru
			ds.ReadXml(rdr, XmlReadMode.ReadSchema);

			return ds;
		}

		public static string DataSetXmlSerialize(DataSet ds)
		{
			StringWriter sw = new StringWriter();
			// write schema so empty elements will pass thru
			ds.WriteXml(sw, XmlWriteMode.WriteSchema);

			// remove null character references
			string retval = sw.ToString().Replace("&#x0;", "");

			return retval;
		}

		#endregion

		#region FormatName()
		public static string FormatName(string Name)
		{
			return FormatName(Name, 0, "");
		}

		public static string FormatName(string Name, int MaxLength)
		{
			return FormatName(Name, MaxLength, "");
		}

		public static string FormatName(string Name, string WildCard)
		{
			return FormatName(Name, 0, WildCard);
		}

		public static string FormatName(string Name, int MaxLength, string WildCard)
		{
			string retval = "";

			retval = CleanString(Name, new char[0]);

			if ( retval.Length > 0 )
			{
				if ( MaxLength == 0 )
				{
					int pos = retval.IndexOf(" ");
					if ( pos > 0 )
					{
						retval = retval.Substring(0, pos) + WildCard;
					}
				}
				else
				{
					MaxLength = retval.Length < MaxLength ? retval.Length : MaxLength;

					retval = retval.Substring(0, MaxLength) + WildCard;
				}
			}

			return retval;
		}
		#endregion

		#region SplitPhoneNumber()
		public static void SplitPhoneNumber(string inCombinedPhone, ref string outAreaCode, ref string outPhoneNumber)
		{
			inCombinedPhone = Utilities.CleanNumeric(inCombinedPhone);

			if ( inCombinedPhone.Length >= 10 )
			{
				outAreaCode = inCombinedPhone.Substring(0, 3);
				outPhoneNumber = inCombinedPhone.Substring(3, 7);
			}
			else if ( inCombinedPhone.Length == 9 )
			{
				outAreaCode = inCombinedPhone.Substring(0, 2);
				outPhoneNumber = inCombinedPhone.Substring(2, 7);
			}
			else if ( inCombinedPhone.Length == 8 )
			{
				outAreaCode = inCombinedPhone.Substring(0, 1);
				outPhoneNumber = inCombinedPhone.Substring(1, 7);
			}
			else
			{
				outAreaCode = "";
				outPhoneNumber = inCombinedPhone;
			}

			if ( outAreaCode.Length > 0 && Convert.ToInt32(outAreaCode) == 0 )
				outAreaCode = "";

			if ( outPhoneNumber.Length > 0 && Convert.ToInt32(outPhoneNumber) == 0 )
				outPhoneNumber = "";

			return;
		}
		#endregion

		#region SplitLocality()
		public static void SplitLocality(string inCombinedLocality, char inCityStateDelimiter, ref string outCity, ref string outState)
		{
			int pos = inCombinedLocality.LastIndexOf(inCityStateDelimiter);

			if ( pos > 0 )
			{
				outState = inCombinedLocality.Substring(pos + 1).Trim();
				outCity = inCombinedLocality.Substring(0, pos).Trim();
			}
			else
			{
				outState = inCombinedLocality.Trim();
				outCity = "";
			}
		}

		public static void SplitLocality(string inCombinedLocality, char inCityStateDelimiter, char inStateZipCodeDelimiter,
			ref string outCity, ref string outState, ref string outZipCode)
		{
			int pos = inCombinedLocality.LastIndexOf(inStateZipCodeDelimiter);

			if ( pos > 0 )
			{
				outZipCode = inCombinedLocality.Substring(pos + 1).Trim();
				inCombinedLocality = inCombinedLocality.Substring(0, pos).Trim();
				SplitLocality(inCombinedLocality, inCityStateDelimiter, ref outCity, ref outState);
			}
			else
			{
				outZipCode = inCombinedLocality.Trim();
				outState = "";
				outCity = "";
			}
		}
		#endregion

		#region CleanNumeric()
		public static string CleanNumeric(string val)
		{
			// replaced cleanup routime with regex
			return Regex.Replace(val, @"\D", "");
		}
		#endregion

		#region CleanString()
		public static string CleanString(string val)
		{
			return CleanString(val, BAD_CHARS_NULL, BAD_CHARS_SPACE.ToCharArray());
		}

		public static string CleanString(string val, string BadCharsNull)
		{
			return CleanString(val, BadCharsNull, BAD_CHARS_SPACE.ToCharArray());
		}

		public static string CleanString(string val, char[] BadCharsSpace)
		{
			return CleanString(val, BAD_CHARS_NULL, BadCharsSpace);
		}

        public static string CleanString(string val, string BadCharsNull, char[] BadCharsSpace)
        {
            // convert to using regex
            // fixed bug in special character pattern
            string specialChars = @"\\|\$|\.|\||\{|\[|\]|\(|\)|\*|\+|\?|\^";

            // convert char array to string
            string badCharsSpace = new string(BadCharsSpace);

            // escape all necesary special characters
            if (Regex.IsMatch(badCharsSpace, specialChars))
            {
                badCharsSpace = Regex.Replace(badCharsSpace, specialChars, @"\$0");
            }

            // escape all necesary special characters
            if (Regex.IsMatch(BadCharsNull, specialChars))
            {
                BadCharsNull = Regex.Replace(BadCharsNull, specialChars, @"\$0");
            }

            string retval = val;
            if (badCharsSpace.Length > 1) retval = Regex.Replace(retval, @"[" + badCharsSpace + "]", " ");
            if (BadCharsNull.Length > 1) retval = Regex.Replace(retval, @"[" + BadCharsNull + "]", "");

            return retval.Trim();
        }

		#endregion

		#region ParseDate()

		/// <summary>
		/// A value to logically denote NULL for use with SQL data that does not allow NULLS.
		/// </summary>
		public static DateTime SQL_NULL_DATE = DateTime.ParseExact(
			"1900-01-01 00:00:00.000",
			"yyyy-MM-dd HH:mm:ss.fff",
			System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>
		/// The minimum acceptable value allowed for the SQL Server datetime data type.
		/// </summary>
		public static DateTime SQL_MIN_DATE = DateTime.ParseExact(
			"1753-01-01 00:00:00.000",
			"yyyy-MM-dd HH:mm:ss.fff",
			System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>
		/// The maximum acceptable value allowed for the SQL Server datetime data type.
		/// </summary>
		public static DateTime SQL_MAX_DATE = DateTime.ParseExact(
			"9999-12-31 11:59:59.999",
			"yyyy-MM-dd HH:mm:ss.fff",
			System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>
		/// Parses a date string with a variety of known formats. See Utilities.ParseDate() for supported formats.
		/// </summary>
		/// <param name="Date">Date string to parse.</param>
		/// <param name="DefaultValue">Value to return if parse fails or parsed value is out of range.</param>
		/// <returns>Returns the parsed value as a DateTime or DefaultValue if the parse fails.</returns>
		public static object ParseSqlDate(string Date, object DefaultValue)
		{
			DateTime dt = Utilities.ParseDate(Date);

			object retval = dt;
			if ( dt.CompareTo(DateTime.MinValue) == 0 ||
				dt.CompareTo(Utilities.SQL_NULL_DATE) == 0 ||
				dt.CompareTo(Utilities.SQL_MIN_DATE) < 0 ||
				dt.CompareTo(Utilities.SQL_MAX_DATE) > 0 )
			{
				retval = DefaultValue;
			}

			return retval;

		}

		/// <summary>
		/// Attempts to parse a date string with a variety of known goofy formats.
		/// </summary>
		/// <param name="Date">Date string to parse.</param>
		/// <returns>Returns the parsed value or DateTime.MinValue if the parse fails.</returns>
		public static DateTime ParseDate(string date)
		{
			// A standard TryParse() will be attempted before the following TryParseExact() formats...
			//		yyyyMMdd
			//		MMddyyyy
			//		ddMMyyyy
			//		MM/yyyy
			//		yyyy/MM
			//		yyMMdd
			//		MMddyy
			//		ddMMyy
			//		yyyyMM
			//		MMyyyy
			DateTime dt = DateTime.MinValue;

			// first see if it will parse in any format
			if ( DateTime.TryParse(date, out dt) == false )
			{
				// try formats based on lengths
				if ( date.Length == 8 )
				{
					// 8-digit formats
					string[] formats = new string[] { "yyyyMMdd", "MMddyyyy", "ddMMyyyy" };

					DateTime.TryParseExact(
						date,
						formats,
						System.Globalization.CultureInfo.InvariantCulture,
						System.Globalization.DateTimeStyles.None,
						out dt);
				}
				else if ( date.Length == 7 )
				{
					// added 7-digit formats
					string[] formats = new string[] { "MM/yyyy", "yyyy/MM" };

					DateTime.TryParseExact(
						date,
						formats,
						System.Globalization.CultureInfo.InvariantCulture,
						System.Globalization.DateTimeStyles.None,
						out dt);
				}
				else if ( date.Length == 6 )
				{
					// 6-digit formats
					string[] formats = new string[] { "yyMMdd", "MMddyy", "ddMMyy", "yyyyMM", "MMyyyy" };

					DateTime.TryParseExact(
						date,
						formats,
						System.Globalization.CultureInfo.InvariantCulture,
						System.Globalization.DateTimeStyles.None,
						out dt);
				}
				else
				{
					// don't have any clue what format this is
				}
			}

			return dt;
		}

        #endregion

        #region Misc. File Functions

        public static void ArchiveFile(string file, string archiveLocation)
        {
            ArchiveFile(file, archiveLocation, false);
        }

        public static void ArchiveFile(string file, string archiveLocation, bool copyOnly)
        {
            Utilities.CreateFolderAsNecessary(archiveLocation);

            FileInfo info = new FileInfo(file);
            if (!copyOnly)
            {
                // append backslash if necessary
                if (archiveLocation.Length > 0 && archiveLocation.Substring(archiveLocation.Length - 1, 1) != @"\")
                {
                    archiveLocation += @"\";
                }
                string destination = archiveLocation + info.Name;

                FileInfo destInfo = new FileInfo(destination);
                if (destInfo.Exists) destInfo.Delete();
                destInfo = null;

                info.MoveTo(destination);
            }
            else
            {
                info.CopyTo(archiveLocation + info.Name, true);
            }
        }

        // disk space function
        /// <summary>
        /// Gets the availble free space in bytes from any networked server drive.
        /// </summary>
        /// <param name="UncPath">The path to the server drive in UNC notation</param>
        /// <returns>Available space in bytes</returns>
        public static long GetAvailableDiskSpace(string UncPath)
		{
			long lBytesAvailable = -1;
			long lTotalBytes;
			long lTotalFreeBytes;
			IntPtr pszPath = Marshal.StringToHGlobalAuto(UncPath);

			try
			{
				int iVal = Win32SDK.GetDiskFreeSpaceEx(pszPath, out lBytesAvailable, out lTotalBytes, out lTotalFreeBytes);
			}
			finally
			{
				Marshal.FreeHGlobal(pszPath);
			}

			return lBytesAvailable;
		}

		public static void WriteMessageToLogFile(string Filename, string Message)
		{
			StreamWriter sw = null;

			try
			{
				using ( sw = File.AppendText(Filename) )
				{
					sw.WriteLine("{0} {1}\t{2}",
						DateTime.Now.ToShortDateString(),
						DateTime.Now.ToShortTimeString(),
						Message);

					sw.Close();
				}
			}
			catch
			{
				if ( sw != null )
					sw.Close();
			}
		}

		public static string GetExecutingFolder()
		{
			string retval = Assembly.GetExecutingAssembly().Location;

			return retval.Substring(0, retval.LastIndexOf('\\') + 1);
		}

		public static bool CanOpenFile(string file)
		{
			bool retval;

			try
			{
				using ( FileStream fs = File.Open(file, FileMode.Append, FileAccess.Write, FileShare.None) )
				{
					fs.Close();
					retval = true;
				}
			}
			catch
			{
				retval = false;
			}

			return retval;
		}

		public static void CreateFolderAsNecessary(string FolderToCheck)
		{
			DirectoryInfo di = new DirectoryInfo(FolderToCheck);

			if ( !di.Exists )
			{
				CreateFolderAsNecessary(di.Parent.FullName);
				di.Create();
			}
		}

		#endregion
	}
}