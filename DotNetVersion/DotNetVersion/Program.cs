using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;

// Source of code samples:
// https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed

// The trick here is to build an app with the lowest possible .NET 4.x Version while be able to display the highest possible.
// The problem is that we must use .NET 4.0 instead of .NET 3.5 because some server's released since 2016 do not install
// .NET 3.5 by default, so there is no guarantee it is there or that this app will run.  
// Thus we use .NET 4.0 as the minimal version that can detect all other 4.x versions.

namespace DotNetVersion
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"MachineName: {Environment.MachineName}"); 
            Console.WriteLine($"   OSVersion: {Environment.OSVersion}");
            Console.WriteLine($"   Is64BitOS: {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"Common Langauge Runtime Version: {Environment.Version}");
            Console.WriteLine(GetDotNet4xVersion());
            Console.WriteLine($"{DateTime.Now} {TimeZoneInfo.Local}");
            Console.WriteLine($"    Run by: {Environment.UserDomainName}\\{Environment.UserName}");

            // Since I execute this from Windows File Explorer by double-clicking,
			// I will pause the Console Window so that is does not abruptly close.
			Console.WriteLine();
			Console.WriteLine("Press ENTER key to close.");
			Console.ReadLine();
        }

        private static string GetDotNet4xVersion()
        {
            string version = Get45PlusFromRegistry();
            if (string.IsNullOrWhiteSpace(version))
            {
                version = GetEarlierVersionFromRegistry();
            }
            return version;
        }

        private static string Get45PlusFromRegistry()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return $".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}";
                }
                else
                {
                    return null;
                }
            }

            // Checking the version using >= enables forward compatibility.
            string CheckFor45PlusVersion(int releaseKey)
            {
                if (releaseKey >= 528040)
                    return "4.8 or later";
                if (releaseKey >= 461808)
                    return "4.7.2";
                if (releaseKey >= 461308)
                    return "4.7.1";
                if (releaseKey >= 460798)
                    return "4.7";
                if (releaseKey >= 394802)
                    return "4.6.2";
                if (releaseKey >= 394254)
                    return "4.6.1";
                if (releaseKey >= 393295)
                    return "4.6";
                if (releaseKey >= 379893)
                    return "4.5.2";
                if (releaseKey >= 378675)
                    return "4.5.1";
                if (releaseKey >= 378389)
                    return "4.5";
                // This code should never execute. A non-null release key should mean
                // that 4.5 or later is installed.
                return "No 4.5 or later version detected";
            }
        }

        private static string GetEarlierVersionFromRegistry()
        {
            var builder = new StringBuilder();
            // Opens the registry key for the .NET Framework entry.
            using (RegistryKey ndpKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).
                    OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                foreach (var versionKeyName in ndpKey.GetSubKeyNames())
                {
                    // Skip .NET Framework 4.5 version information.
                    if (versionKeyName == "v4")
                    {
                        continue;
                    }

                    if (versionKeyName.StartsWith("v"))
                    {

                        RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                        // Get the .NET Framework version value.
                        var name = (string)versionKey.GetValue("Version", "");
                        // Get the service pack (SP) number.
                        var sp = versionKey.GetValue("SP", "").ToString();

                        // Get the installation flag, or an empty string if there is none.
                        var install = versionKey.GetValue("Install", "").ToString();
                        if (string.IsNullOrEmpty(install)) // No install info; it must be in a child subkey.
                            builder.AppendLine($"{versionKeyName}  {name}");
                        else
                        {
                            if (!(string.IsNullOrEmpty(sp)) && install == "1")
                            {
                                builder.AppendLine($"{versionKeyName}  {name}  SP{sp}");
                            }
                        }
                        if (!string.IsNullOrEmpty(name))
                        {
                            continue;
                        }
                        foreach (var subKeyName in versionKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (!string.IsNullOrEmpty(name))
                                sp = subKey.GetValue("SP", "").ToString();

                            install = subKey.GetValue("Install", "").ToString();
                            if (string.IsNullOrEmpty(install)) //No install info; it must be later.
                                builder.AppendLine($"{versionKeyName}  {name}");
                            else
                            {
                                if (!(string.IsNullOrEmpty(sp)) && install == "1")
                                {
                                    builder.AppendLine($"{subKeyName}  {name}  SP{sp}");
                                }
                                else if (install == "1")
                                {
                                    builder.AppendLine($"  {subKeyName}  {name}");
                                }
                            }
                        }
                    }
                }
                return builder.ToString();
            }
        }
    }
}
