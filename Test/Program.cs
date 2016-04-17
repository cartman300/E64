using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using E64;

namespace Test {
	class Program {
		[DllImport("user32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		static extern void MessageBox(IntPtr Handle, string Text, string Caption, int Options);

		static void Dump(byte[] Bytes) {
			const string Name = "Elisa.bin";

			if (File.Exists(Name))
				File.Delete(Name);
			File.WriteAllBytes(Name, Bytes);
		}

		static void Main(string[] args) {
			Console.Title = "Test";

			Assembler Prog = new Assembler();
			Prog.Instr(Instruction.PUSH_I64).Int64(0);
			Prog.Instr(Instruction.PUSH_STR).Data("Hello VM World!");
			Prog.Instr(Instruction.PUSH_STR).Data("Message Box");
			Prog.Instr(Instruction.PUSH_I32).Int32(1);

			Prog.Instr(Instruction.PINVOKE).Data("user32").Data("MessageBox")
				.Int8((byte)CharSet.Auto)
				.Int8((byte)CallingConvention.Winapi)
				.Data(typeof(int).FullName)
				.Int8(255);
				/*.Int8(4)
				.Data(typeof(IntPtr).FullName)
				.Data(typeof(string).FullName)
				.Data(typeof(string).FullName)
				.Data(typeof(int).FullName);//*/

			Prog.Instr(Instruction.HALT);

			byte[] ProgMem = Prog.ToByteArray();
			Dump(ProgMem);

			CPU Elisa = new CPU(ProgMem);
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