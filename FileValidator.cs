using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Framework
{
	/// <summary>
	/// Supports methods for validating files within the context of the NSTN Batch processing experiences.
	/// </summary>
	public class FileValidator
	{
		#region Private Members

		private const int EXPECTED_VALUE = 0;
		private const int ACTUAL_VALUE = 1;

		private int[] mFieldCounts = new int[2] { 0, 0 };
		private FileTypes[] mFileTypes = new FileTypes[2] { FileTypes.Unknown, FileTypes.Unknown };

		#endregion

		#region Constructors

		/// <summary>
		/// FileValidator Constructor
		/// </summary>
		/// <param name="Filename">Full path and file name to validate.</param>
		/// <param name="FileType">The expected FileValidator.FileTypes of the file.</param>
		/// <param name="FieldCount">The expected number of fields in a record.</param>
		public FileValidator(string Filename, FileTypes FileType, int FieldCount)
			: this(Filename, FileType, FieldCount, null)
		{
		}

		/// <summary>
		/// FileValidator Constructor
		/// </summary>
		/// <param name="Filename">Full path and file name to validate.</param>
		/// <param name="FileType">The expected FileValidator.FileTypes of the file.</param>
		/// <param name="FieldCount">The expected number of fields in a record.</param>
		/// <param name="CheckFields">The optional FileValidator.CheckField descriptors for data content validation.</param>
		public FileValidator(string Filename, FileTypes FileType, int FieldCount, CheckField[] CheckFields)
		{
			this.Filename = Filename;
			this.ExpectedFileType = FileType;
			this.ExpectedFieldCount = FieldCount;
			this.CheckFields = CheckFields;

			ValidationErrors = new List<string>();
		}

		#endregion

		#region Public Members

		public enum FileTypes
		{
			Xls,
			FixedLength,
			CsvDelimited,
			TabDelimted,
			CustomDelimited,
			Xml,
			Zip,
			Pgp,
			UnknownBinary,
			UnknownText,
			UnknownTextSingleRecord,
			UnknownZero,
			Unknown
		}

		/// <summary>
		/// Field descriptors used to perform data content validations.
		/// </summary>
		public struct CheckField
		{
			/// <summary>
			/// The zero-based index of the ordinal position of the field within the record.
			/// </summary>
			public int Index;
			/// <summary>
			/// The common name for the field. Used in error messages.
			/// </summary>
			public string Name;
			/// <summary>
			/// The expected System.Type of the data contained in the field. 
			/// </summary>
			public Type DataType;
			/// <summary>
			/// Flag to indicate requiredness validation.
			/// </summary>
			public bool IsRequired;
		}

		/// <summary>
		/// An optional array of FileValidator.CheckField used to validate the data content of the first record.
		/// </summary>
		public CheckField[] CheckFields = null;

		/// <summary>
		/// Complete path and file name of file to validate.
		/// </summary>
		public string Filename = "";

		/// <summary>
		/// A list of errors that occurred during the validation process.
		/// </summary>
		public List<string> ValidationErrors = null;

		/// <summary>
		/// The number of expected fields in a record 
		/// or the expected length of a fixed length record
		/// </summary>
		public int ExpectedFieldCount 
		{
			get { return mFieldCounts[EXPECTED_VALUE]; }
			set { mFieldCounts[EXPECTED_VALUE] = value; }
		}

		/// <summary>
		/// The number of actual fields in a record 
		/// or the actual length of a fixed length record
		/// </summary>
		public int ActualFieldCount
		{
			get { return mFieldCounts[ACTUAL_VALUE]; }
			set { mFieldCounts[ACTUAL_VALUE] = value; }
		}

		/// <summary>
		/// The FileValidator.FileTypes the file is expected to validate against.
		/// </summary>
		public FileTypes ExpectedFileType
		{
			get { return mFileTypes[EXPECTED_VALUE]; }
			set { mFileTypes[EXPECTED_VALUE] = value; }
		}

		/// <summary>
		/// The actual FileValidator.FileTypes of the validated file.
		/// </summary>
		public FileTypes ActualFileType
		{
			get { return mFileTypes[ACTUAL_VALUE]; }
			set { mFileTypes[ACTUAL_VALUE] = value; }
		}

		// header bypass flag
		/// <summary>
		/// The number of header rows to skip
		/// </summary>
		public int SkipHeaderRows = 0;

		#endregion

		#region Public Methods

		/// <summary>
		/// This instance method attempts to validate the file based on the established expected parameters. 
		/// Its simplistic implementation is focused on the file types that are typical for the
		/// NSTN Batch Process.
		/// </summary>
		/// <returns>Success</returns>
		public bool PerformValidation()
		{
			return PerformValidation("");
		}

		/// <summary>
		/// This instance method attempts to validate the file based on the established expected parameters. 
		/// Its simplistic implementation is focused on the file types that are typical for the
		/// NSTN Batch Process.
		/// </summary>
		/// <param name="CustomDelimiter">A delimiter other than comma or tab to seek in evaluated file.</param>
		/// <returns>Success</returns>
		public bool PerformValidation(string CustomDelimiter)
		{
			try
			{
				ValidationErrors.Clear();

				this.ActualFileType = determineFileType(this.Filename, CustomDelimiter);
				// ignore/allow single record files with the assumption they are of the correct type
				if ( this.ActualFileType == FileTypes.UnknownTextSingleRecord ) this.ActualFileType = this.ExpectedFileType;

				if ( this.ActualFileType != this.ExpectedFileType )
				{
					string msg = string.Format("Expected file type [{0}] does not match actual file type [{1}] for file [{2}].",
						this.ExpectedFileType,
						this.ActualFileType,
						this.Filename);
					// check for zero byte files - make special error
					if ( this.ActualFileType == FileTypes.UnknownZero )
					{
						msg = string.Format("File [{0}] does not does not contain any data (zero length).", 
							this.Filename);
					}
					ValidationErrors.Add(msg);
				}

				if ( ValidationErrors.Count == 0 )
				{
					switch ( this.ActualFileType )
					{
						case FileTypes.CsvDelimited:
						case FileTypes.CustomDelimited:
						case FileTypes.FixedLength:
						case FileTypes.TabDelimted:
							bool success = validateRows(this.ActualFileType, CustomDelimiter);
							break;
					}
				}
			}
			catch ( Exception ex )
			{
				ValidationErrors.Add(ex.ToString());
			}

			return ( ValidationErrors.Count == 0 );
		}

		/// <summary>
		/// This static method attempts to determine the type of the file. Its simplistic
		/// implementation is focused on the file types that are typical for the
		/// NSTN Batch Process.
		/// </summary>
		/// <param name="Filename">Fully-qualified path and file name to evaluate.</param>
		/// <returns>An enumerated value of type Framework.FileValidator.FileTypes.</returns>		
		public static FileTypes PerformFileTypeDetermination(string Filename)
		{
			return determineFileType(Filename, "");
		}

		/// <summary>
		/// This static method attempts to determine the type of the file. Its simplistic
		/// implementation is focused on the file types that are typical for the
		/// NSTN Batch Process.
		/// </summary>
		/// <param name="Filename">Fully-qualified path and file name to evaluate.</param>
		/// <param name="CustomDelimiter">A delimiter other than comma or tab to seek in evaluated file. Blank if fixed-length.</param>
		/// <returns>An enumerated value of type Framework.FileValidator.FileTypes.</returns>		
		public static FileTypes PerformFileTypeDetermination(string Filename, string CustomDelimiter)
		{
			return determineFileType(Filename, CustomDelimiter);
		}

		#endregion

		#region Private Methods

		private static FileTypes determineFileType(string filename, string customDelimiter)
		{
			const int BUFFER_SIZE = 100;

			FileInfo fi = new FileInfo(filename);
			// if the file is not at least BUFFER_SIZE, consider it an unknown(bad) file
			if ( fi.Length >= BUFFER_SIZE )
			{
				// grab a "piece" of the file
				byte[] buff = new byte[BUFFER_SIZE];
				using ( FileStream fs = fi.OpenRead() )
				{
					fs.Read(buff, 0, buff.Length);
				}

				/////////////////////////////////////////////////////////////////////////////////////
				/////////////////////////////////////////////////////////////////////////////////////
				// Order of execution is important - DO NOT CHANGE UNLESS A BUG HAS BEEN DETECTED! //
				/////////////////////////////////////////////////////////////////////////////////////
				/////////////////////////////////////////////////////////////////////////////////////

				// let's evaluate the ascii representation
				string strBuff = Encoding.UTF7.GetString(buff);

				// look closer at the ascii representation to see if we have any non-ascii characters
				bool isAscii = true;
				for ( int i = 0; i < strBuff.Length; i++ )
				{
					if ( (int)strBuff[i] == 0 || (int)strBuff[i] > 127 )
					{
						isAscii = false;
						break;
					}
				}

				// if we verified this is completely ascii, determine text type
				if ( isAscii )
				{
					// look for simple, obvious stuff
					if ( strBuff.IndexOf("BEGIN PGP") >= 0 ) return FileTypes.Pgp;
					if ( strBuff.ToLower().IndexOf("<xml>") >= 0 ) return FileTypes.Xml;
					// read in up to 5 lines and check if equal length...
					if ( isFixedLength(filename) ) return FileTypes.FixedLength;
					// adjust ascii file type based on delimiter
					if ( customDelimiter.Length > 0 && strBuff.IndexOf(customDelimiter) >= 0 ) return FileTypes.CustomDelimited;
					if ( strBuff.IndexOf('\t') >= 0 ) return FileTypes.TabDelimted;
					if ( strBuff.IndexOf(',') >= 0 ) return FileTypes.CsvDelimited;
					// can't make a determination from a single record
					if ( isSingleRecordTextFile(filename) ) return FileTypes.UnknownTextSingleRecord;
					// if we got here it's an unknown text type
					return FileTypes.UnknownText;
				}
				else
				{
					// let's see if it's an xls file
					if ( isXls(strBuff.Substring(0, 8)) ) return FileTypes.Xls;
					// let's see if it's a zip file
					if ( isZip(strBuff.Substring(0, 8)) ) return FileTypes.Zip;
					// well, it's a binary file of some type
					return FileTypes.UnknownBinary;
				}
			}
			else if ( fi.Length == 0 )
			{
				// check for zero length file
				return FileTypes.UnknownZero;
			}

			return FileTypes.Unknown;
		}

		private static bool isSingleRecordTextFile(string filename)
		{
			Encoding UNICODE_PAGE = Encoding.GetEncoding("ISO-8859-1");

			int i = 0;
			using ( StreamReader sr = new StreamReader(filename, UNICODE_PAGE) )
			{
				while ( sr.Peek() >= 0 && i++ < 2 )
				{
					string buff = sr.ReadLine();
				}
			}

			return ( i == 1 );
		}

		private static bool isFixedLength(string filename)
		{
			Encoding UNICODE_PAGE = Encoding.GetEncoding("ISO-8859-1");

			bool retval = false;
			string prev = "";
			int i = -1;

			using ( StreamReader sr = new StreamReader(filename, UNICODE_PAGE) )
			{
				while ( sr.Peek() >= 0 && i++ < 5 )
				{
					string buff = sr.ReadLine();

					// ignore blank lines since they don't provide meaningful data
					if ( buff.Length > 0 )
					{
						retval = ( buff.Length == prev.Length );
						if ( i > 0 && !retval ) break;

						prev = buff;
					}
					else
					{
						i--;
					}
				}
			}

			// let's do a check here to make sure we're 
			// not fooled by a 1-3 record, tab-delimited file
			if ( retval == true && i < 3 && prev.Split("\t".ToCharArray()).Length > 5 )
			{
				retval = false;
			}

			return retval;
		}

		private static bool isXls(string header)
		{
			const string TEST = "\xD0\xCF\x11\xE0\xA1\xB1\x1A\xE1";

			return ( header == TEST );
		}

		private static bool isZip(string header)
		{
			const string TEST1 = "\x50\x4B\x03\x04\x14\x00\x00\x00";
			const string TEST2 = "\x50\x4B\x03\x04\x14\x00\x02\x00";

			return ( header == TEST1 || header == TEST2 );
		}

		private bool validateRows(FileTypes fileType, string customDelimiter)
		{
			// assume succes
			int errors = 0;

			bool isFixedLength = false;
			string delimiter = "";

			switch ( fileType )
			{
				case FileTypes.CsvDelimited:
					delimiter = ",";
					break;
				case FileTypes.TabDelimted:
					delimiter = "\t";
					break;
				case FileTypes.CustomDelimited:
					delimiter = customDelimiter;
					break;
				case FileTypes.FixedLength:
					delimiter = "";
					isFixedLength = true;
					break;
				default:
					throw new ApplicationException(
						string.Format("File type [{0}] is not supported for all row validations in file [{1}].",
							fileType,
							this.Filename)
					);
				//break;
			}

			// open the file for integrity check
			// no nested try/catch - let the parent try/catch handle any error
			using ( FileReader fr = new FileReader(this.Filename, delimiter, isFixedLength) )
			{
				int headerCount = 0;

				while ( fr.LoadNextLine() )
				{
					// skip header rows
					if ( this.SkipHeaderRows <= headerCount )
					{
						if ( !isFixedLength )
						{
							this.ActualFieldCount = fr.FieldCount;
						}
						else
						{
							this.ActualFieldCount = fr.Record.Length;
						}

						if ( this.ActualFieldCount != this.ExpectedFieldCount )
						{
							string msg = string.Format("File [{0}] is corrupt. Actual field count/length [{1}] at line [{2}] does not match expected field count/length [{3}].",
								this.Filename,
								this.ActualFieldCount,
								fr.RecordCount,
								this.ExpectedFieldCount);
							ValidationErrors.Add(msg);

							errors++;
						}

						// verify data type values in the first record, if requested
						if ( fr.RecordCount == 1 && errors == 0 && this.CheckFields != null )
						{
							for ( int i = 0; i < this.CheckFields.Length; i++ )
							{
								string dataValue = fr.GetField(this.CheckFields[i].Index);

								errors += validateField(dataValue, this.CheckFields[i]);

								if ( errors == 5 ) break;
							}
						}

						// the whole file could be out of whack, so stop after five errors
						if ( errors == 5 ) break;
					}
					else
					{
						headerCount++;
					}
				}
			}

			return ( errors == 0 );
		}

		private int validateField(string dataValue, CheckField chkFld)
		{
			int retval = 0;

			if ( dataValue.Length > 0 )
			{
				// if the expected data type is a string, there's nothing to do
				if ( dataValue.GetType() != chkFld.DataType )
				{
					// special value handling is necessary if the expected data type is a DateTime
					if ( chkFld.DataType == typeof(DateTime) )
					{
						// try to parse the data value first to guard against known goofy formats
						DateTime tempDate = Utilities.ParseDate(dataValue);
						if ( tempDate.CompareTo(DateTime.MinValue) != 0 )
						{
							dataValue = tempDate.ToShortDateString();
						}
					}

					// try to convert the value to the expected data type
					try
					{
						object obj = Convert.ChangeType(dataValue, chkFld.DataType);
					}
					catch
					{
						string msg = string.Format("File [{0}] is corrupt. The value [{1}], in the field [{2}], at position [{3}], does not match the expected validation data type of [{4}].",
							this.Filename,
							dataValue,
							chkFld.Name,
							chkFld.Index,
							chkFld.DataType.ToString());
						
						ValidationErrors.Add(msg);
						
						retval = 1;
					}
				}
			}
			else
			{
				if ( chkFld.IsRequired )
				{
					string msg = string.Format("File [{0}] is corrupt. The field [{1}], at position [{2}], has a \"Data Is Required For Validation\" flag set and there is no field data value.",
						this.Filename,
						chkFld.Name,
						chkFld.Index);
					
					ValidationErrors.Add(msg);
					
					retval = 1;
				}
			}

			return retval;
		}

		#endregion
	}
}
