using System.Diagnostics;

namespace bootwrapper;

public static class Program {
 
    public async static Task<int> Main(string[] args) {
        if(args.Length < 1)
            throw new ArgumentException($"\n\n* This tool requires at least one argument, example: `bootwrapper {{keyname(s)}}`\n* You can pass in unlimited desired keys, separated by spaces.\n");
        try {
            using (Process process = new()) {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = "run";

                string[] keys = await Task.WhenAll(args.Select(GetKeychainValue));
                for(int cursor = 0; cursor < args.Length; cursor++)
                    process.StartInfo.Environment[args[cursor]] = keys[cursor];

                process.Start();
                process.WaitForExit();
            }
            return 0;
        } catch (Exception exception) {
            Console.WriteLine($"\n* Exception encountered attempting to build and run project:\n{exception}\n");
            return 1;
        }
    }

    private static async Task<string> GetKeychainValue(string name) {
        using (Process securityProcess = new() {
            StartInfo = new ProcessStartInfo() {
                FileName = "/usr/bin/security", // default macos keychain exec location.
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        }) {
            securityProcess.StartInfo.ArgumentList.Add("find-generic-password"); // macos keychain cli command.
            securityProcess.StartInfo.ArgumentList.Add($"-l"); // match by label.
            securityProcess.StartInfo.ArgumentList.Add($"{name}"); // label.
            securityProcess.StartInfo.ArgumentList.Add($"-w"); // only output the password value.

            securityProcess.Start();
            string result = (await securityProcess.StandardOutput.ReadToEndAsync()).Trim();
            await securityProcess.WaitForExitAsync();

            if(securityProcess.ExitCode != 0) {
                string error = (await securityProcess.StandardError.ReadToEndAsync()).Trim();
                throw new InvalidOperationException($"\n* Key {name} was not found in Keychain Access, failed to boot.\nError returned:\n{error}");
            }

            return result;
        }
    }

}
