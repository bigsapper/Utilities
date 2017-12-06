using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Framework
{
	// class for defining extension methods
	public static class Extensions
	{
		#region String Class Extensions

		#region IsGuid()

		/// <summary>
		/// Determines if the string value is a valid Guid value
		/// </summary>
		/// <param name="s">This string instance</param>
		/// <param name="value">The Guid value of the parsed string; Will equal Guid.Empty if parsing fails</param>
		/// <returns>True if successfully parsed</returns>
		public static bool IsGuid(this string s, out Guid value)
		{
			//ClsidFromString returns the empty guid for null strings    
			if ( ( s == null ) || ( s.IsEmptyOrWhitespace() ) )
			{
				value = Guid.Empty;
				return false;
			}

			int hresult = PInvoke.ObjBase.CLSIDFromString(s, out value);
			if ( hresult >= 0 )
			{
				return true;
			}
			else
			{
				value = Guid.Empty;
				return false;
			}
		}

		/// <summary>
		/// Determines if the string value is a valid Guid value
		/// </summary>
		/// <param name="s">This string instance</param>
		/// <returns>True if successfully parsed</returns>
		public static bool IsGuid(this string s)
		{
			Guid value = Guid.Empty;

			return s.IsGuid(out value);
		}

		#endregion

		#region IsDate()

		/// <summary>
		/// Determines if the string value is a valid DateTime value
		/// </summary>
		/// <param name="s">This string instance</param>
		/// <param name="dt">DateTime value of the parsed string; Will equal DateTime.MinValue if the string cannot be parsed</param>
		/// <returns>True if it is a valid DateTime value; False if not</returns>
		public static bool IsDate(this string s, out DateTime dt)
		{
			// added to fix OpenDate bug
			bool retval = false;
			dt = DateTime.MinValue;

			if ( s.IsEmptyOrWhitespace() ) return retval;

			try
			{
				dt = DateTime.Parse(s);
				retval = true;
			}
			catch
			{
				try
				{
					// this is a special case that won't automatically parse
					dt = DateTime.ParseExact(s, "yyyyMMdd", null);
					retval = true;
				}
				catch
				{
					retval = false;
					dt = DateTime.MinValue;
				}
			}

			return retval;
		}

		/// <summary>
		/// Determines if the string value is a valid DateTime value
		/// </summary>
		/// <param name="s">This string instance</param>
		/// <returns>True if it is a valid DateTime value; False if not</returns>
		public static bool IsDate(this string s)
		{
			DateTime dt;

			return IsDate(s, out dt);
		}

		#endregion

		#region IsNumeric()

		/// <summary>
		/// Determines if a string is a valid numeric value, floating point or integer
		/// </summary>
		/// <param name="s">This string instance</param>
		/// <returns>True if a valid numeric value</returns>
		public static bool IsNumeric(this string s)
		{
			bool retval = false;

			if ( s.IsEmptyOrWhitespace() ) return retval;

			double result;
			retval = Double.TryParse(s, System.Globalization.NumberStyles.Any, null, out result);

			return retval;
		}

		#endregion

		#region IsEmptyOrWhitespace()

		public static bool IsEmptyOrWhitespace(this string text)
		{
			// Avoid creating iterator for trivial case 
			if ( text.Length == 0 ) return true;

			foreach ( char c in text )
			{
				if ( Char.IsWhiteSpace(c) ) continue;
				return false;
			}
			return true;
		}

		#endregion

		#region MaxLength(int Length)

		// string extension method
		public static string MaxLength(this string text, int Length)
		{
			string retval = text.Trim();

			if ( retval.Length > Length ) retval = retval.Substring(0, Length);

			return retval;
		}

		#endregion

		#region IsNullOrEmpty

		public static bool IsNullOrEmpty(this string val)
		{
			// added for bug fix support
			return string.IsNullOrEmpty(val);
		}

		#endregion

		#endregion

		#region Object Class Extensiosn

		#region FormatString()

		/// <summary>
		/// Formats the object accounting for the various forms of "null"; Also removes tabs (\t) and commas (,)
		/// </summary>
		/// <param name="o">This object instance</param>
		/// <returns>The formatted string</returns>
		public static string FormatString(this object o)
		{
			string retval = "";

			if ( o != null && o != DBNull.Value && o.ToString().ToLower() != "null" )
			{
				retval = Convert.ToString(o).Trim();
			}

			retval = retval.Replace("\t", "").Replace(",", " ");

			return retval;
		}

		/// <summary>
		/// Formats the object accounting for the various forms of "null"; Also removes tabs (\t) and commas (,)
		/// </summary>
		/// <param name="o">This object instance</param>
		/// <param name="MaxLength">The maximum length og the formatted string</param>
		/// <returns>The formatted string</returns>
		public static string FormatString(this object o, int MaxLength)
		{
			string retval = o.FormatString();

			if ( retval.Length > MaxLength )
				retval = retval.Substring(0, MaxLength);

			return retval;
		}

		#endregion

		#region DBString()

		/// <summary>
		/// Formats the object for storage as a valid string for a database field
		/// </summary>
		/// <param name="o">This object instance</param>
		/// <returns>The formatted string</returns>
		public static string GetDBString(this object o)
		{
			return DBString(o, false, false);
		}

		/// <summary>
		/// Formats the object for storage as a valid string for a database field
		/// </summary>
		/// <param name="o">This object instance</param>
		/// <param name="UseNull">If true, uses the string "NULL" for an empty string</param>
		/// <param name="AddQuotes">If true, surrounds string with single quotation marks</param>
		/// <param name="MaxLength">Maximum length of the formatted string</param>
		/// <returns>The formatted string</returns>
		public static string GetDBString(this object o, bool UseNull)
		{
			return DBString(o, UseNull, true);
		}

		/// <summary>
		/// Formats the object for storage as a valid string for a database field
		/// </summary>
		/// <param name="o">This object instance</param>
		/// <param name="UseNull">If true, uses the string "NULL" for an empty string</param>
		/// <param name="AddQuotes">If true, surrounds string with single quotation marks</param>
		/// <param name="MaxLength">Maximum length of the formatted string</param>
		/// <returns>The formatted string</returns>
		public static string DBString(this object o, int MaxLength)
		{
			return DBString(o, false, false, MaxLength);
		}

		public static string DBString(this object o, bool UseNull, bool AddQuotes)
		{
			return DBString(o, UseNull, AddQuotes, -1);
		}

		/// <summary>
		/// Formats the object for storage as a valid string for a database field
		/// </summary>
		/// <param name="o">This object instance</param>
		/// <param name="UseNull">If true, uses the string "NULL" for an empty string. Default is false.</param>
		/// <param name="AddQuotes">If true, surrounds string with single quotation marks. Default is false.</param>
		/// <param name="MaxLength">Maximum length of the formatted string. Default is -1 (no truncation).</param>
		/// <returns>The formatted string</returns>
		public static string DBString(this object o, bool UseNull, bool AddQuotes, int MaxLength)
		{
			string retval = "";

			if ( o != null )
			{
				retval = Convert.ToString(o).Replace("\0", "").Replace("'", "''").Trim();
			}

			if ( retval.Length > 0 )
			{
				if ( MaxLength > 0 && retval.Length > MaxLength )
				{
					// CLG: what if the last character at truncation is a single-quote?
					// it was already doubled up but the truncation removes it
					retval = retval.Substring(0, MaxLength);
				}

				if ( AddQuotes == true )
				{
					retval = "'" + retval + "'";
				}
			}
			else
			{
				retval = UseNull ? "Null" : AddQuotes ? "''" : retval;
			}

			return retval;
		}

        public static object DBNullStringCheck(this object o)
        {
            object retval = null;

            if ( o != null )
            {
                string s = Convert.ToString(o).Replace("\0", "").Replace("'", "''").Trim();
                if (s.Length > 0)
                {
                    retval = s;
                }
                else
                {
                    retval = DBNull.Value;
                }
            }
            else
            {
                retval = DBNull.Value;
            }

            return retval;
        }

		#endregion

		#endregion

		// CLG: add new methods from the utilities class and deprecate them
	}
}

namespace PInvoke
{
	class ObjBase
	{
		/// <summary> 
		/// This function converts a string generated by the StringFromCLSID function back into the original class identifier. 
		/// </summary> 
		/// <param name="sz">String that represents the class identifier</param> 
		/// <param name="clsid">On return will contain the class identifier</param> 
		/// <returns> 
		/// Positive or zero if class identifier was obtained successfully 
		/// Negative if the call failed 
		/// </returns> 
		[DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = true)]
		public static extern int CLSIDFromString(string sz, out Guid clsid);
	}
}
