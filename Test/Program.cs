using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E64;

namespace Test {
	class Program {
		static void Main(string[] args) {
			Console.Title = "Test";

			byte[] Memory = new byte[4096];

			CPU Elisa = new CPU(new Indexable<ulong, byte>((Addr) => {
				return Memory[Addr];
			}, (Addr, Val) => {
				Memory[Addr] = Val;
			}));

			while (true)
				Elisa.Step();

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}