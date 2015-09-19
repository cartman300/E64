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

			List<byte> Prog = new List<byte>();
			Prog.Add((byte)Instruction.MOVE_CONST);
			Prog.Add(6);
			Prog.AddRange(BitConverter.GetBytes((long)42069));
			Prog.Add((byte)Instruction.MOVE_REG);
			Prog.Add(0);
			Prog.Add(6);
			Prog.Add((byte)Instruction.PRINT_GP);
			Prog.Add(0);
			Prog.Add((byte)Instruction.INT_CONST);
			Prog.Add(42);
			Prog.Add((byte)Instruction.PRINT_GP);
			Prog.Add(0);
			Prog.Add((byte)Instruction.HALT);

			byte[] Memory = Prog.ToArray();
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