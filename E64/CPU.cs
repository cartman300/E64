using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace E64 {
	// _REG _I8 _I16 _I32 _I64
	public enum Instruction : byte {
		NOP,
		HALT,
		INT_I8,
		JUMP_I64,
		JUMP_REG,
		MOVE_REG_I64,
		MOVE_REG_REG,
		PUSH_STR,
		PUSH_I64,
		PUSH_I32,
		PINVOKE,
	}

	public unsafe class CPU {
		public bool Halted;
		//Indexable<UInt64, byte> Memory;
		byte[] Memory;

		public event Action<byte> OnInterrupt;
		public Registers Regs;
		public Stack<object> Stack;

		public CPU(byte[] Mem) {
			Halted = false;
			Regs = new Registers();
			Stack = new Stack<object>();
			Memory = Mem;
		}

		/*public CPU(byte[] Memory) : this(new Indexable<ulong, byte>((K) => Memory[K], (K, V) => Memory[K] = V)) {
		}*/

		public byte FetchInstrInt8() {
			return Memory[Regs.IP++];
		}

		public Int16 FetchInstrInt16() {
			return BitConverter.ToInt16(new[] { FetchInstrInt8(), FetchInstrInt8() }, 0);
		}

		public Int32 FetchInstrInt32() {
			byte[] IntBytes = new[] { FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8() };
			return BitConverter.ToInt32(IntBytes, 0);
		}

		public Int64 FetchInstrInt64() {
			byte[] IntBytes = new[] { FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(),
				FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8() };
			return BitConverter.ToInt64(IntBytes, 0);
		}

		public string FetchInstrString() {
			ulong Addr = (ulong)FetchInstrInt64();
			int Len = BitConverter.ToInt32(new[] { Memory[Addr], Memory[Addr + 1], Memory[Addr + 2], Memory[Addr + 3] }, 0);

			byte[] Bytes = new byte[Len];
			for (int i = 0; i < Len; i++)
				Bytes[i] = Memory[4 + (ulong)i + Addr];
			return Encoding.UTF8.GetString(Bytes);
		}

		public void Interrupt(byte Num) {
			if (OnInterrupt != null)
				OnInterrupt(Num);
			Halted = false;
		}

		public void Step() {
			if (Halted)
				return;

			Instruction I = (Instruction)FetchInstrInt8();
			switch (I) {
				case Instruction.NOP:
					break;
				case Instruction.HALT:
					Halted = true;
					break;
				case Instruction.INT_I8:
					Interrupt(FetchInstrInt8());
					break;
				case Instruction.JUMP_I64:
					Regs.IP = (UInt64)FetchInstrInt64();
					break;
				case Instruction.JUMP_REG:
					Regs.IP = (UInt64)Regs.GP[FetchInstrInt8()];
					break;
				case Instruction.MOVE_REG_I64:
					Regs.GP[FetchInstrInt8()] = FetchInstrInt64();
					break;
				case Instruction.MOVE_REG_REG:
					Regs.GP[FetchInstrInt8()] = Regs.GP[FetchInstrInt8()];
					break;
				case Instruction.PUSH_STR:
					Stack.Push(FetchInstrString());
					break;
				case Instruction.PUSH_I64:
					Stack.Push(FetchInstrInt64());
					break;
				case Instruction.PUSH_I32:
					Stack.Push(FetchInstrInt32());
					break;
				case Instruction.PINVOKE: {
						string LibName = FetchInstrString();
						string FuncName = FetchInstrString();
						CharSet CSet = (CharSet)FetchInstrInt8();
						CallingConvention CConv = (CallingConvention)FetchInstrInt8();

						Type ReturnType = Type.GetType(FetchInstrString());
						int ParamTypeCount = FetchInstrInt8();
		
						Type[] ParamTypes = null;
						if (ParamTypeCount == 255)
							ParamTypes = new Type[Stack.Count];
						else
							ParamTypes = new Type[ParamTypeCount];

						if (ParamTypeCount != 255)
							for (int i = 0; i < ParamTypes.Length; i++)
								ParamTypes[i] = Type.GetType(FetchInstrString());
						else {
							object[] StackVals = Stack.Reverse().ToArray();
							for (int i = 0; i < ParamTypes.Length; i++)
								ParamTypes[i] = StackVals[i].GetType();
						}

						Console.WriteLine("[DllImport(\"{0}\", CharSet = {1}, CallingConvention = {2})]", LibName, CSet, CConv);
						Console.WriteLine("{0} {1}({2})", ReturnType, FuncName, string.Join(", ", ParamTypes.Select((_) => _.ToString())));

						List<object> Args = new List<object>();
						for (int i = ParamTypes.Length - 1; i >= 0; i--)
							Args.Add(ChangeType(Stack.Pop(), ParamTypes[i]));
						Args.Reverse();

						Console.WriteLine("{0}({1})", FuncName, string.Join(", ", Args));
						break;
					}

				default:
					throw new Exception("Invalid instruction " + I);
			}
		}

		public object ChangeType(object O, Type T) {
			if (O is Int64 && T == typeof(IntPtr))
				return new IntPtr((Int64)O);
			return Convert.ChangeType(O, T);
		}
	}
}