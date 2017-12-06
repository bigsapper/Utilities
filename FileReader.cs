using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Framework
{
	public class FileReader: IDisposable 
	{
		private StreamReader mStreamReader = null;

		private string		mFilename;			// Path to the target file
		private int			mFieldCount;		// Count of mFields in this record
		private int			mExpectedFieldCount;
		private bool		mStatus;			// Are we able to provide data?
		private string		mRecord;			// Current record
		private Array		mFields;			// Current record field array
		private string		mErrMsg;			// Last error message
		private string		mFieldSeperator;	// User defined field seperator
		private int[]   	mFieldSpecifiers;	// User defined field specifiers for fixed length files
		private bool		mFixedLength;		//
		private int			mRecordCount;

		private readonly Encoding UNICODE_PAGE = Encoding.GetEncoding("ISO-8859-1");	 

		public FileReader(string Filename) : this(Filename, ",", false, 0) {}

		public FileReader(string Filename, string FieldSeperator, bool FixedLength) : this(Filename, FieldSeperator, FixedLength, 0) { }

		public FileReader(string Filename, string FieldSeperator, bool FixedLength, int ExpectedFieldCount)
		{
			mStatus = AccessTargetFile(Filename);
			mFieldSeperator = FieldSeperator;
			mFixedLength = FixedLength;
			mExpectedFieldCount = ExpectedFieldCount;
		}

		// Return the record string
		public string Record
		{
			get 
			{
				return mRecord;
			}
		}

		public int RecordCount
		{
			get
			{
				return mRecordCount;
			}
		}

		// Return the message string
		public string ErrorMessage
		{
			get
			{
				return mErrMsg;
			}
		}

		// Sget the field delimiter character. Default is the comma.
		public string FieldSeperator
		{
			get
			{
				return mFieldSeperator;
			}
			set
			{
				mFieldSeperator = value;
			}
		}

		// Set the field specifiers for fixed length files
		public int[] FieldSpecifiers
		{
			get
			{
				return mFieldSpecifiers;
			}
			set
			{
				mFieldSpecifiers = value;
				mFixedLength = true;
			}
		}

		// resolves fixed record length
		public int ExpectedFixedRecordLength
		{
			get
			{
				int retval = 0;
				
				for ( int i = 0; i < mFieldSpecifiers.Length; i ++ )
				{
					retval += mFieldSpecifiers[i];
				}

				return retval;
			}
		}

		// Tell caller the status
		public bool Status
		{
			get
			{
				return mStatus;
			}
		}

		// Give out the number of mFields in this record
		public int FieldCount
		{
			get 
			{
				return mFieldCount;
			}
		}

		// Return the filename
		public string Filename
		{
			get
			{
				return mFilename;
			}
		}

		// Reads the next line of text and parses it into mFields array
		public bool LoadNextLine()
		{
			// clean up routine logic from here on down
			// assume false
			bool retval = false;

			this.mRecord = "";
			this.mFields = new string[0];
			this.mFieldCount = 0;

			try
			{
				if ( this.mStreamReader.Peek() >= 0 )
				{
					this.mRecordCount++;
					// read as Unicode and convert to ASCII
					string unicodeBuffer = this.mStreamReader.ReadLine();
					// perform the conversion from one encoding to the other
					byte[] asciiBytes = Encoding.Convert(UNICODE_PAGE, Encoding.ASCII, UNICODE_PAGE.GetBytes(unicodeBuffer));
					// convert the new ascii byte array into a string
					this.mRecord = Encoding.ASCII.GetString(asciiBytes);

					if ( this.mRecord.Trim().Length > 0 )
					{
						this.mFields = ParseLine();
						this.mFieldCount = mFields.Length;
					}

					retval= true;
				}
			}
			catch ( Exception ex )
			{
				throw new ApplicationException(String.Format("Cannot read file {0} at line {1}. File is corrupt.", 
					this.mFilename, 
					mRecordCount), ex);
			}

			return retval;
		}

		// Pass back the specified field
		public string GetField(int Index)
		{
			if ( Index < 0 || Index >= mFieldCount )
				return "";
			else
				return mFields.GetValue(Index).ToString();
		}

		public void CloseFile()
		{
			try
			{
				if ( mStreamReader != null ) mStreamReader.Close();
			}
			catch
			{
				// do nothing
			}
			finally
			{
				this.mErrMsg = "";
				this.mExpectedFieldCount = 0;
				this.mFieldCount = 0;
				this.mFields = new string[0];
				this.mFieldSeperator = "";
				this.mFieldSpecifiers = new int[0];
				this.mFilename = "";
				this.mFixedLength = false;
				this.mRecord = "";
				this.mRecordCount = 0;
				this.mStatus = false;
				this.mStreamReader = null;
			}
		}

		// Pass back the specified field
		private bool AccessTargetFile(string filename)
		{
			// asuume fail
			bool retval = false;
			mErrMsg = "";

			try
			{
				this.CloseFile();

				// open as Unicode to prevent foreign character data loss
				mStreamReader = new StreamReader(filename,  UNICODE_PAGE);
				mFilename = filename;

				retval = true;
			}
			catch ( Exception ex )
			{
				mErrMsg = ex.ToString();
				retval = false;
			}

			return retval;
		}

		private Array ParseLine()
		{
			ArrayList flds = null;

			if ( !this.mFixedLength )
			{
				flds = ParseDelimitedLine(this.mRecord);
			}
			else
			{
				flds = ParseFixedLengthLine(this.mRecord);
			}
						
			// check to see if we parsed the line correctly
			if ( this.mExpectedFieldCount > 0 && this.mExpectedFieldCount != flds.Count )
			{
				if ( this.mExpectedFieldCount > flds.Count )
				{
					// Let's just add the missing fields to the array and be done with it
					while ( this.mExpectedFieldCount > flds.Count )
					{
						flds.Add("");
						// make sure the original record has the additional fields also
						if ( !this.mFixedLength )
						{
							this.mRecord += this.mFieldSeperator;
						}
					}

					// make sure the original fixed length record has the additional length also
					if ( this.mFixedLength && ( this.ExpectedFixedRecordLength > this.mRecord.Length ) )
					{
						this.mRecord += new String(' ', this.ExpectedFixedRecordLength - mRecord.Length);
					}
				}
				else
				{
					throw new Exception(String.Format("Expected field count {0} does not match field count of parsed record {1}.", 
						this.mExpectedFieldCount, 
						flds.Count));
				}
			}

			return flds.ToArray(typeof(string));
		}

		private ArrayList ParseDelimitedLine(string lineBuffer)
		{
			string[] temp = lineBuffer.Split(mFieldSeperator[0]);
			
			ArrayList retval = new ArrayList();

			for (int i = 0; i < temp.Length; i++ )
			{
				// if leading quote
				if ( temp[i].Length > 0 && temp[i][0] == '"' )
				{
					string fld = temp[i].Substring(1).Trim();
					// bug fix: don't assume that there is non-whitespace data to either side of an embedded delimiter
					if ( fld.Length == 0 || fld[fld.Length - 1] != '"' )
					{
						// look for trailing quote
						while ( i + 1 < temp.Length && temp[i + 1][temp[i + 1].Length - 1] != '"' ) 
						{
							i++;
							fld += mFieldSeperator + temp[i];
						}
						// add last segment
						i++;
						fld += mFieldSeperator + temp[i];
					}
					retval.Add(fld.Substring(0, fld.Length - 1).Trim());
				}
				else
				{
					retval.Add(temp[i].Trim());
				}
			}

			return retval;
		}

		private ArrayList ParseFixedLengthLine(string lineBuffer)
		{
			ArrayList retval = new ArrayList(mFieldSpecifiers.Length);

			int startIndex = 0;
			for ( int i = 0; i < mFieldSpecifiers.Length; i ++ )
			{
				if ( i > 0 )
				{
						startIndex += mFieldSpecifiers[i - 1];
				}

				// if record length is short of the start index, let's just skip it
				if ( lineBuffer.Length >= startIndex + mFieldSpecifiers[i] )
				{
					retval.Add(lineBuffer.Substring(startIndex, mFieldSpecifiers[i]).Trim());
				}
			}

			return retval;
		}

		#region IDisposable Members

		public void Dispose()
		{
			this.CloseFile();
		}
		
		#endregion
	}
}
