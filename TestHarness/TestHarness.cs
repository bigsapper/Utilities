/*
********************************************************************************
** MODULE:       TestHarness
** FILENAME:     TestHarness.cs
** AUTHOR:       Chris Gallucci
*********************************************************************************
**
** DESCRIPTION:
** Console application template for debugging and testing utilties.
** Created using Microsoft Visual C#.
**
*********************************************************************************
*/
using System;
using System.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;

using Framework;

namespace Framework
{
	/// <summary>
	/// Summary description for TestHarness.
	/// </summary>
	class TestHarness
	{
		///// <summary>
        ///// The main entry point for the application.
        ///// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// always show running version
			Console.WriteLine(System.Environment.Version);

			try
			{
				string address1 = "PO Box 413 824 Hwy 13 N";
				string address2 = "";

				Framework.ParseAddress addyParser = new Framework.ParseAddress(true);
				Framework.ParseAddress.ParsedAddress addy = addyParser.Parse(address1, address2);

				Console.WriteLine();
				Console.WriteLine("Address Parser...");
				Type type = addy.GetType();
				FieldInfo[] props = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
				foreach ( FieldInfo prop in props )
				{
					Console.WriteLine(String.Format("\tThe value of {0} is: {1}",
						prop.Name,
						prop.GetValue(addy)));
				}

				Console.WriteLine();
			}
			catch ( Exception ex )
			{
				Console.WriteLine();
				Console.WriteLine(ex.ToString());
			}
			finally
			{
				// done; waiting to terminate
				Console.Write("Press any key to quit...");
				Console.Read();
			}
		}
	}
}
