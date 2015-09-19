using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using E64;

namespace InstructionSetGenerator {
	class Program {
		static StringBuilder Out;

		static string SuffixToString(string Sfx, ref int ArgCnt) {
			switch (Sfx) {
				case "REG":
					return "{" + ArgCnt++ + "}";
				case "I8":
					return "{" + ArgCnt++ + "}";
				case "I16":
					return "num {" + ArgCnt++ + "}, 2";
				case "I32":
					return "num {" + ArgCnt++ + "}, 4";
				case "I64":
					return "num {" + ArgCnt++ + "}, 8";
				default:
					throw new Exception("Unknown suffix " + Sfx);
			}
		}

		static string ToInstrDef(byte Val, string Name, out int ArgCnt) {
			string[] Mods = Name.Split('_');
			string Ret = Val.ToString();

			ArgCnt = 0;
			for (int i = 1; i < Mods.Length; i++)
				Ret += " " + SuffixToString(Mods[i], ref ArgCnt);

			return Ret;
		}

		static string GenerateArgs(int Cnt) {
			List<string> ArgNames = new List<string>();
			for (int i = 0; i < Cnt; i++)
				ArgNames.Add(((char)('A' + i)).ToString());
			string Args = " " + string.Join(", ", ArgNames.ToArray());

			if (Cnt > 0)
				Args += " ";
			return Args;
		}

		static void Main(string[] args) {
			Console.Title = "Instruction Set Generator";
			Out = new StringBuilder();
			Out.AppendLine("atomic macro num n, bytes { if (bytes > 0) { (n >> ((bytes - 1) * 8)) & 0xFF num n, bytes - 1 } }");

			string[] Instrs = Enum.GetNames(typeof(Instruction));
			for (int i = 0; i < Instrs.Length; i++) {
				int ArgCnt;
				string InstrDef = ToInstrDef((byte)(Instruction)Enum.Parse(typeof(Instruction), Instrs[i]), Instrs[i], out ArgCnt);
				string Fmt2 = string.Format("atomic macro {0}{1}{{{{ {2} }}}}\n", Instrs[i], GenerateArgs(ArgCnt), InstrDef);
				Out.AppendFormat(Fmt2, "A", "B", "C", "D", "E", "F", "G", "H", "I", "J");
			}

			File.WriteAllText("E64_InstrSet.asm", Out.ToString());
			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}