using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pocioci.VS
{
    public class Executable
    {
        public Executable(string path, string workingDirectory)
        {
            Path = path;
            WorkingDirectory = workingDirectory;
            EnvironmentVariables = new Dictionary<string, string>();
            Encoding = Encoding.UTF8;
        }

        public bool IsAvailable
        {
            get
            {
                return File.Exists(Path);
            }
        }

        public string WorkingDirectory { get; private set; }
        public string Path { get; private set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public Encoding Encoding { get; set; }

        public ExecutableResult Execute(string arguments, params object[] args)
        {
            var process = CreateProcess(arguments, args);
            process.Start();

            Func<StreamReader, string> reader = (StreamReader streamReader) => streamReader.ReadToEnd();

            IAsyncResult outputReader = reader.BeginInvoke(process.StandardOutput, null, null);
            IAsyncResult errorReader = reader.BeginInvoke(process.StandardError, null, null);

            process.StandardInput.Close();

            process.WaitForExit();

            string output = reader.EndInvoke(outputReader);
            string error = reader.EndInvoke(errorReader);

            if (process.ExitCode != 0)
            {
                string text = String.IsNullOrEmpty(error) ? output : error;

                throw new Exception(text);
            }

            return new ExecutableResult
            {
                Output = output,
                Error = error
            };
        }

        private Process CreateProcess(string arguments, object[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path,
                WorkingDirectory = WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                ErrorDialog = false,
                Arguments = String.Format(arguments, args)
            };

            if (Encoding != null)
            {
                psi.StandardOutputEncoding = Encoding;
                psi.StandardErrorEncoding = Encoding;
            }

            foreach (var pair in EnvironmentVariables)
            {
                psi.EnvironmentVariables[pair.Key] = pair.Value;
            }

            var process = new Process()
            {
                StartInfo = psi
            };

            return process;
        }
    }
}