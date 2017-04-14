using System;
using System.IO;
using System.Reflection;
using NDesk.Options;

namespace tModProcessorEx
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine("{0} v{1}{2}", typeof(Program).Namespace,
				Assembly.GetExecutingAssembly().GetName().Version.ToString(3),
				Environment.NewLine);

			Console.WriteLine("args: {0}", string.Join(" ", args));

			string path = null, mode = "dump", folder = null;
			var dllonly = false;

			var options = new OptionSet
			{
				{"f|file=", "Mod file path", v => path = v },
				{"m|mode=", "Running mode", v => mode = v },
				{"p|folder=", "patch folder", v => folder = v },
				{"dllonly", v => dllonly = true }
			};

			options.Parse(args);

			if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(mode) ||
				mode.Equals("patch", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(folder))
			{
				options.WriteOptionDescriptions(Console.Out);
			}
			else
			{
				switch (mode.ToUpperInvariant())
				{
					case "DUMP":
						DumpModFile(path, dllonly);
						break;
					case "PATCH":
						PatchModFile(path, folder);
						break;
					default:
						Console.Error.WriteLine("Invalid mode entered! Modes: dump, patch");
						break;
				}
			}
		}

		private static void DumpModFile(string path, bool dllOnly = true)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("Invalid file path!");
				return;
			}

			var tmod = new TmodFile(path);
			tmod.Read();

			try
			{
				Directory.Delete(tmod.Name, true);
			}
			catch
			{
				// ignored
			}

			var dir = Directory.CreateDirectory(tmod.Name).FullName;

			if (dllOnly)
			{
				var dll = tmod.GetMainAssemblyPath();
				File.Copy(dll, Path.Combine(dir, "Windows.dll"), true);
			}
			else
			{
				foreach (var file in tmod)
				{
					Console.WriteLine("Writing {0}", file.Key);
					var currentPath = Path.Combine(dir, file.Key);
					var currentDirectory = Path.GetDirectoryName(currentPath);
					if (!string.IsNullOrWhiteSpace(currentDirectory))
						Directory.CreateDirectory(currentDirectory);

					File.WriteAllBytes(currentPath, file.Value);
				}
			}
		}

		private static void PatchModFile(string path, string folder)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("Invalid file path!");
				return;
			}

			var directory = new DirectoryInfo(folder);
			if (!directory.Exists)
			{
				Console.WriteLine("Invalid folder path!");
				return;
			}

			var tmod = new TmodFile(path);
			tmod.Read();

			var root = new Uri(directory.FullName + (directory.FullName.EndsWith("\\") ? string.Empty : "\\"));

			InternalWrite(tmod, directory);

			tmod.Save(tmod.Name + "_patched.tmod");

			void InternalWrite(TmodFile mod, DirectoryInfo current)
			{
				foreach (var sub in current.EnumerateDirectories())
				{
					InternalWrite(mod, sub);
				}

				foreach (var file in current.EnumerateFiles())
				{
					var f = new Uri(file.FullName);
					var r = root.MakeRelativeUri(f);

					mod.SetFile(r.ToString(), File.ReadAllBytes(file.FullName));
				}
			}
		}
	}
}
