using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Framework
{
	#region Name Parsing Class

	public sealed class ParseName
	{
		public string NamePrefix = "";
		public string FirstName = "";
		public string MiddleName = "";
		public string LastName = "";
		public string NameSuffix = "";
		public string MergedName = "";

		public ParseName(DataRow row)
		{
			int idx = new int();

			idx = row.Table.Columns.IndexOf("FirstName");
			if ( idx >= 0 ) this.FirstName = row[idx].ToString().Trim();

			idx = row.Table.Columns.IndexOf("MiddleName");
			if ( idx >= 0 ) this.MiddleName = row[idx].ToString().Trim();

			idx = row.Table.Columns.IndexOf("LastName");
			if ( idx >= 0 ) this.LastName = row[idx].ToString().Trim();

			idx = row.Table.Columns.IndexOf("NamePrefix");
			if ( idx >= 0 ) this.NamePrefix = row[idx].ToString().Trim();

			idx = row.Table.Columns.IndexOf("NameSuffix");
			if ( idx >= 0 ) this.NameSuffix = row[idx].ToString().Trim();

			this.MergedName = CreateMergedName(
				this.NamePrefix,
				this.FirstName,
				this.MiddleName,
				this.LastName,
				this.NameSuffix
			);
		}

		public ParseName(string NamePrefix, string FirstName, string MiddleName, string LastName, string NameSuffix)
		{
			this.NamePrefix = NamePrefix.Trim();
			this.FirstName = FirstName.Trim();
			this.MiddleName = MiddleName.Trim();
			this.LastName = LastName.Trim();
			this.NameSuffix = NameSuffix.Trim();

			this.MergedName = CreateMergedName(
				this.NamePrefix,
				this.FirstName,
				this.MiddleName,
				this.LastName,
				this.NameSuffix
			);
		}

		public ParseName(string CombinedName)
		{
			bool lastNameIsFirst = bLastNameIsFirst();
			InitializeObject(CombinedName, lastNameIsFirst);
		}

		public ParseName(string CombinedName, bool LastNameIsFirst)
		{
			InitializeObject(CombinedName, LastNameIsFirst);
		}

		private void InitializeObject(string combinedName, bool lastNameIsFirst)
		{
			// cleanup and working assignment
			while ( combinedName.IndexOf(" ,") >= 0 )
			{
				combinedName = combinedName.Replace(" ,", ",");
			}
			if ( combinedName.IndexOf("AKA ") == 0 && combinedName.Length > 4 )
			{
				combinedName = combinedName.Substring(4);
			}
			if ( combinedName.IndexOf(" ???") >= 0 )
			{
				combinedName = combinedName.Replace(" ???", "");
			}
			// BK name cleanup
			if ( combinedName.IndexOf("RESP PTY FOR ") >= 0 )
			{
				combinedName = combinedName.Replace("RESP PTY FOR ", "");
			}
			if ( combinedName.IndexOf("RESP PARTY ") >= 0 )
			{
				combinedName = combinedName.Replace("RESP PARTY ", "");
			}
			if ( combinedName.IndexOf("RESPT PARTY ") >= 0 )
			{
				combinedName = combinedName.Replace("RESPT PARTY ", "");
			}

			// Returns a description of the next person referenced in the name specification 
			sElements = combinedName.Split(' ');
			iElement = 0;

			if ( lastNameIsFirst )
			{
				ParseLastNameFirstName();
			}
			else
			{
				ParseFirstNameLastName();
			}

			this.MergedName = CreateMergedName(
				this.NamePrefix,
				this.FirstName,
				this.MiddleName,
				this.LastName,
				this.NameSuffix
			);
		}

		private string CreateMergedName(string prefix, string fName, string mName, string lName, string suffix)
		{
			string retval = "";

			if ( mName.Length > 0 )
			{
				retval = string.Format("{0} {1} {2} {3} {4}",
					prefix,
					fName,
					mName,
					lName,
					suffix);
			}
			else
			{
				retval = string.Format("{0} {1} {2} {3}",
					prefix,
					fName,
					lName,
					suffix);
			}

			return retval.Trim();
		}

		/* 
		' Rough BNF syntax:- 
		'    namespec = fore [middlenames] lastname 
		'    fore = prefix [firstname] | firstname 
		'    join = and | & 
		'    prefix = Mr. | Mrs. | etc... 
		'    firstname = name | initial. 
		'    middlename = name | initial. 
		'    middlenames = middlename middlenames | $ 
		'    lastname = lastname ',' | lastname suffix | $ 
		'    suffix = Jr. | Sr. | etc... 
		 */
		// Array of symbols from the current name specification 
		private string[] sElements;
		// Index of element currently being processed
		private int iElement;

		// Tests the name parts order
		private bool bLastNameIsFirst()
		{
			bool retval = false;

			// check to see if the last name is first
			if ( this.MergedName.IndexOf(',') >= 0 )
			{
				if ( this.MergedName.IndexOf(',') < this.MergedName.IndexOf(' ') )
				{
					retval = true;
				}
				else if ( sElements.Length > 1 && bSuffixNext(1) )
				{
					retval = true;
				}
			}

			return retval;
		}

		// Tests whether the next element is a known suffix 
		private bool bSuffixNext(int i)
		{
			int tmp = iElement;
			iElement = i;

			bool retval = bSuffixNext();

			iElement = tmp;

			return retval;
		}

		private bool bSuffixNext()
		{
			bool retval = false;

			if ( iElement < sElements.Length )
			{
				string tmp = sElements[iElement].Trim().ToLower().Replace(",", "").Replace(".", "");
				switch ( tmp )
				{
					case "jr":
					case "sr":
					case "esq":
					case "md":
					case "dds":
					case "ii":
					case "2":
					case "2nd":
					case "iii":
					case "3":
					case "3rd":
					case "iv":
					case "4":
					case "4th":
					// case "v": can't use because it's more prevalent as a middle initial
					case "5":
					case "5th":
					case "phd":
						retval = true;
						break;
					default:
						break;
				}
			}
			return retval;
		}

		// Tests whether the next element is a known prefix 
		private bool bPrefixNext(int i)
		{
			int tmp = iElement;
			iElement = i;

			bool retval = bPrefixNext();

			iElement = tmp;

			return retval;
		}

		private bool bPrefixNext()
		{
			bool retval = false;

			if ( iElement < sElements.Length )
			{
				string tmp = sElements[iElement].Trim().ToLower().Replace(".", "");
				switch ( tmp )
				{
					case "dr":
					case "ml":
					case "mr":
					case "mrs":
					case "ms":
					case "hon":
					case "rep":
					case "rn":
					case "rv":
					case "sen":
					case "ss":
						retval = true;
						break;
					default:
						break;
				}
			}

			return retval;
		}

		// Tests whether the next element is a known join 
		private bool bJoinNext()
		{
			bool retval = false;

			if ( iElement < sElements.Length )
			{
				switch ( sElements[iElement].Trim().ToLower() )
				{
					case "and":
					case "&":
						retval = true;
						break;
					default:
						break;
				}
			}

			return retval;
		}

		// Consumes and returns the next name 
		private string sConsumeName()
		{
			string retval = "";
			if ( iElement < sElements.Length )
			{
				retval = sElements[iElement].Trim();
				iElement++;
			}

			return retval;
		}

		private void ParseFirstNameLastName()
		{
			string sName = "";

			// Deal with 'fore' 
			if ( bPrefixNext() )
			{
				this.NamePrefix = sConsumeName();

				if ( !bJoinNext() )
				{
					this.FirstName = sConsumeName();
				}
			}
			else
			{
				this.FirstName = sConsumeName();
			}

			// Now deal with one or more middlenames, up to the last name (possible with a suffix) 
			while ( iElement < sElements.Length && !bJoinNext() )
			{
				// If suffix next then we must already have the lastname in sName 
				if ( bSuffixNext() )
				{
					this.LastName = sName.Replace(",", ""); ;
					this.NameSuffix = sConsumeName().Replace(",", "");
					break;
				}
				else
				{
					// Accumulate middlenames 
					if ( sName.Length > 0 )
					{
						this.MiddleName += this.MiddleName.Length > 0 ? " " : "" + sName;
					}
					sName = sConsumeName();
				}
			}

			// If we haven't read as far as a suffix then sName will have a middlename or lastname in it 
			if ( 0 == this.NameSuffix.Length )
			{
				this.LastName = sName.Replace(",", "");
				// this was actaully a non-comma-delimited last name first
				if ( this.LastName.Length == 1 && this.MiddleName.Length > 1 )
				{
					// switch
					string tmp = this.LastName;
					this.LastName = this.FirstName.Replace(",", "");
					this.FirstName = this.MiddleName;
					this.MiddleName = tmp;
				}
			}
			else
			{
				// this was actaully a non-comma-delimited last name first
				if ( this.LastName.Length == 0 && iElement < sElements.Length )
				{
					// switch
					this.LastName = this.FirstName.Replace(",", "");
					this.FirstName = sConsumeName();

					// Now deal with one or more middlenames
					while ( iElement < sElements.Length && !bJoinNext() )
					{
						sName = sConsumeName();
						if ( sName.Length > 0 )
						{
							this.MiddleName += this.MiddleName.Length > 0 ? " " : "" + sName;
						}
					}
				}
			}

			// check for another bad format
			if ( sElements[0].Length == 1 && this.FirstName.Length == 1 && this.MiddleName.Length > 1 && this.LastName.Length > 1 )
			{
				// switch
				string tmp = this.FirstName;
				this.FirstName = this.LastName;
				this.LastName = this.MiddleName;
				this.MiddleName = tmp;
			}

			// Check for any diminutive names elements (starting with lowercase) that need moving to lastname
			if ( this.MiddleName.Length > 0 && this.LastName.Length > 0 )
			{
				string[] sMiddleBits = this.MiddleName.Split(' ');

				for ( int i = sMiddleBits.Length - 1; i >= 0; i-- )
				{
					if ( sMiddleBits[i].Length > 0 && !Regex.IsMatch(sMiddleBits[i].Substring(0, 1), "[a-z]") ) break;

					this.LastName = sMiddleBits[i] + " " + this.LastName;
					sMiddleBits[i] = "";
				}

				this.MiddleName = String.Join(" ", sMiddleBits).Trim();
			}

			if ( this.LastName.Length == 1 && this.MiddleName.Length == 0 )
			{
				this.MiddleName = this.LastName;
				this.LastName = "";
			}

			return;
		}

		private void ParseLastNameFirstName()
		{
			string sName = "";

			sName = sConsumeName();
			// Deal with 'aft' 
			if ( bSuffixNext() )
			{
				this.LastName = sName.Replace(",", "");
				this.NameSuffix = sConsumeName().Replace(",", "");
			}
			else
			{
				this.LastName = sName.Replace(",", "");
			}

			// Deal with 'fore' 
			if ( bPrefixNext() )
			{
				this.NamePrefix = sConsumeName();

				if ( !bJoinNext() )
				{
					this.FirstName = sConsumeName();
				}
			}
			else
			{
				this.FirstName = sConsumeName();
			}

			// Now deal with one or more middlenames, up to a pssible suffix
			sName = "";
			while ( iElement < sElements.Length && !bJoinNext() )
			{
				// If suffix next then we must already have the lastname in sName 
				if ( bSuffixNext() )
				{
					// Accumulate middlenames 
					if ( sName.Length > 0 )
					{
						this.MiddleName += this.MiddleName.Length > 0 ? " " : "" + sName;
					}
					this.NameSuffix = sConsumeName();
					break;
				}
				else
				{
					sName = sConsumeName();
					// Accumulate middlenames 
					if ( sName.Length > 0 )
					{
						this.MiddleName += this.MiddleName.Length > 0 ? " " : "" + sName;
					}
				}
			}

			// Check for any diminutive names elements (starting with lowercase) that need moving to lastname
			if ( this.MiddleName.Length > 0 && this.LastName.Length > 0 )
			{
				string[] sMiddleBits = this.MiddleName.Split(' ');

				for ( int i = sMiddleBits.Length - 1; i >= 0; i-- )
				{
					if ( sMiddleBits[i].Length > 0 && !Regex.IsMatch(sMiddleBits[i].Substring(0, 1), "[a-z]") ) break;

					this.LastName = sMiddleBits[i] + " " + this.LastName;
					sMiddleBits[i] = "";
				}

				this.MiddleName = String.Join(" ", sMiddleBits).Trim();
			}

			// check to see if the middle initial was place in the last name field because there is no last name
			if ( this.LastName.Length == 1 && this.MiddleName.Length == 0 )
			{
				this.MiddleName = this.LastName;
				this.LastName = "";
			}

			// see if the prefix is actually in the suffix position
			if ( bPrefixNext(sElements.Length - 1) && this.MiddleName == sElements[sElements.Length - 1] && this.NamePrefix.Length == 0 )
			{
				this.NamePrefix = this.MiddleName;
				this.MiddleName = "";
			}
			return;
		}
	}

	#endregion
}
