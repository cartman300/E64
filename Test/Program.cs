using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using E64;

namespace Test {
	class Program {
		static void Dump(byte[] Bytes) {
			const string Name = "Elisa.bin";

			if (File.Exists(Name))
				File.Delete(Name);
			File.WriteAllBytes(Name, Bytes);
		}

		static void Main(string[] args) {
			Console.Title = "Test";

			byte[] ProgMem = Assembler.Assemble(File.ReadAllText("Test.e64"));
			Dump(ProgMem);

			CPU Elisa = new CPU(ProgMem);
			//Elisa.Debug = Debugger.IsAttached;

			try {
				while (!Elisa.Halted)
					Elisa.Step();
			} catch (Exception E) {
				Console.WriteLine(E.Message);
				throw;
			}
			Console.ReadLine();
		}
	}

	public class Exports {
		public static void Print(string Str) {
			Console.Write(Str);
		}
	}
}