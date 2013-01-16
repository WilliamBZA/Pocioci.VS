using Microsoft.VisualStudio.TextTemplating.VSHost;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace Pocioci.VS
{
    [ComVisible(true)]
    [Guid("4B85C477-71D3-49C6-A3A3-D1BD0749BEAC")]
    public class PociOciCustomTool : VsMultipleFileGenerator<string>
    {
        #region Registration

        // You have to make sure that the value of this field (CustomToolGuid) is exactly 
        // the same as the value of the Guid attribure (at the top of the class)
        private static Guid CustomToolGuid =
            new Guid("{4B85C477-71D3-49C6-A3A3-D1BD0749BEAC}");

        private static Guid CSharpCategory =
            new Guid("{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}");

        private static Guid VBCategory =
            new Guid("{164B10B9-B200-11D0-8C61-00A0C91E29D5}");


        private const string CustomToolName = "Pocioci";

        private const string CustomToolDescription = "Pocioci Generate wrapper for si files";

        private const string KeyFormat
            = @"SOFTWARE\Microsoft\VisualStudio\{0}\Generators\{1}\{2}";

        protected static void Register(Version vsVersion, Guid categoryGuid)
        {
            Debug.WriteLine("++++++++++++++++REGISTER");

            string subKey = String.Format(KeyFormat,
                vsVersion, categoryGuid.ToString("B"), CustomToolName);

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(subKey))
            {
                key.SetValue("", CustomToolDescription);
                key.SetValue("CLSID", CustomToolGuid.ToString("B"));
                key.SetValue("GeneratesDesignTimeSource", 1);
            }
        }

        protected static void Unregister(Version vsVersion, Guid categoryGuid)
        {
            string subKey = String.Format(KeyFormat,
                vsVersion, categoryGuid.ToString("B"), CustomToolName);

            Registry.LocalMachine.DeleteSubKey(subKey, false);
        }

        [ComRegisterFunction]
        public static void RegisterClass(Type t)
        {
            // Register for both VS.NET 2002 & 2003 (C#) 
            Register(new Version(11, 0), CSharpCategory);

            // Register for both VS.NET 2002 & 2003 (VB) 
            Register(new Version(11, 0), VBCategory);
        }

        [ComUnregisterFunction]
        public static void UnregisterClass(Type t)
        { // Unregister for both VS.NET 2002 & 2003 (C#) 
            Unregister(new Version(11, 0), CSharpCategory);

            // Unregister for both VS.NET 2002 & 2003 (VB) 
            Unregister(new Version(11, 0), VBCategory);
        }

        #endregion

        protected byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            Debug.WriteLine(string.Format("Generating code from {0}", inputFileName));

            var tempPath = System.IO.Path.GetTempPath();
            if (tempPath.EndsWith("\\"))
            {
                tempPath = tempPath.Substring(0, tempPath.Length - 1);
            }

            var pocioci = new Executable("pocioci.exe", tempPath);
            pocioci.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("path");
            var result = pocioci.Execute("{0} {1} {2} {3}", "\"-o!targetCSAdoNet=1!CSAdoNetDir=" + tempPath + "\"", "\"" + inputFileName + "\"", "-C-", "-b" + tempPath);

            if (result.Output.StartsWith(inputFileName))
            {
                // Something went wrong
                string lineNumberText = result.Output.Substring(inputFileName.Length + 1);
                lineNumberText = lineNumberText.Substring(0, lineNumberText.IndexOf(")"));
                int lineNumber = Convert.ToInt32(lineNumberText);

                string error = result.Output.Substring(inputFileName.Length + 1);
                error = error.Substring(error.IndexOf(":") + 2);

                //this.GeneratorErrorCallback(false, 0, error, lineNumber, 0);
                
                return Encoding.UTF8.GetBytes(error);
            }
            else
            {
                Debug.WriteLine(string.Format("Execution result status: {0}", result.Error));

                var fileName = Path.GetFileName(inputFileName).Replace(Path.GetExtension(inputFileName), ".cs");

                var filecontent = System.IO.File.ReadAllText(tempPath + "\\" + fileName);

                Debug.WriteLine("Code generation complete");
                return Encoding.UTF8.GetBytes(filecontent);
            }
        }

        public override string GetDefaultExtension()
        { 
            return ".cs";
        }

        public override IEnumerator<string> GetEnumerator()
        {
            var tempPath = System.IO.Path.GetTempPath();
            if (tempPath.EndsWith("\\"))
            {
                tempPath = tempPath.Substring(0, tempPath.Length - 1);
            }

            var pocioci = new Executable("pocioci.exe", tempPath);
            pocioci.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("path");
            var result = pocioci.Execute("{0} {1} {2} {3} {4} {5}", "\"-o!targetCSAdoNet=1!CSAdoNetDir=" + tempPath + "\"", "\"" + InputFilePath + "\"", "-C-", "-b" + tempPath, "-9", "-s" + tempPath);

            if (result.Output.StartsWith(InputFilePath))
            {
                // Something went wrong
                string lineNumberText = result.Output.Substring(InputFilePath.Length + 1);
                lineNumberText = lineNumberText.Substring(0, lineNumberText.IndexOf(")"));
                var lineNumber = Convert.ToUInt32(lineNumberText);

                string error = result.Output.Substring(InputFilePath.Length + 1);
                error = error.Substring(error.IndexOf(":") + 2);

                if (GenerateProgress != null)
                {
                    GenerateProgress.GeneratorError(0, 0, error, lineNumber, 0);
                }
            }
            else
            {
                Debug.WriteLine(string.Format("Execution result status: {0}", result.Error));

                var csFileName = Path.GetFileName(InputFilePath).Replace(Path.GetExtension(InputFilePath), ".cs");
                var sqlFileName = Path.GetFileName(InputFilePath).Replace(Path.GetExtension(InputFilePath), ".sql");

                Debug.WriteLine("Code generation complete");
                yield return csFileName;
                yield return sqlFileName;
            }
        }

        protected override string GetFileName(string element)
        {
            return element.Substring(element.LastIndexOf('/') + 1);
        }

        public override byte[] GenerateContent(string element)
        {
            var tempPath = System.IO.Path.GetTempPath();
            if (tempPath.EndsWith("\\"))
            {
                tempPath = tempPath.Substring(0, tempPath.Length - 1);
            }

            var filecontent = System.IO.File.ReadAllText(tempPath + "\\" + element);
            return Encoding.UTF8.GetBytes(filecontent);
        }

        public override byte[] GenerateSummaryContent()
        {
            // Im not going to put anything in here...
            return new byte[0];
        }
    }
}