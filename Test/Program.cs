using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using E64;

namespace Test {
	class Program {
		static void Main(string[] args) {
			Console.Title = "Test";

			byte[] Memory = File.ReadAllBytes("Elisa.bin");

			Indexable<ulong, byte> Mem = new Indexable<ulong, byte>((K) => Memory[K], (K, V) => Memory[K] = V);
			CPU Elisa = new CPU(Mem);
			Elisa.OnInterrupt += (Int) => {
				Elisa.Regs.GP[0] = 42;
				Console.WriteLine("Interrupt {0}", Int);
			};

			while (!Elisa.Halted)
				Elisa.Step();

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}