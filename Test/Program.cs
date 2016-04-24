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

			Instruction[] Unused;
			byte[] ProgMem = Assembler.Assemble(File.ReadAllText("Test.e64"), out Unused);
			Dump(ProgMem);

			//Console.WriteLine("Unused:");
			//Console.WriteLine(string.Join(", ", Unused));

			CPU Elisa = new CPU(ProgMem);
			Elisa.Ports.Add(0, (Data, IsRead) => {
				Console.Write((char)Data);
				return 0;
			});

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