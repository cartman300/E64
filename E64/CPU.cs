using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using ObjectStack = E64.Stack<object>;

namespace E64 {
	// _REG _I8 _I16 _I32 _I64
	public enum Instruction : byte {
		NOP = 1,
		HALT,
		RESET,

		INT_8,          // INT A - interrupts with A
		SHDLR,          // SIH A B - set interrupt handler B for interrupt A

		CMP_R_R,    // COMPARE A B - compares A and B and sets the flags register
		CMP_R_64,    // COMPARE A B - compares A and B and sets the flags register

		JMP_64,        // JUMP A - jumps to A
		JMP_R,
		JEQ_64,          // JEQ A - jumps if equal to A
		JNE_64,          // JNE A - jumps if not equal to A
		JLE_64,          // JLE A - jumps if less than or equal to A 
		JGE_64,          // JGE A - jumps if greater or equal to A
		JL_64,          // JLE A - jumps if less than or equal to A 
		JG_64,          // JGE A - jumps if greater or equal to A

		ADD_R_R,        // ADD A B - adds A and B and stores in A
		ADD_R_64,        // ADD A B - adds A and B and stores in A
		SUB_R_R,        // DEC A B - adds A and B and stores in A
		SUB_R_64,        // DEC A B - adds A and B and stores in A

		MOV_R_64,      // MOVE A B - moves B into A
		MOV_R_R,

		PUSH_64,        // PUSH A - pushes A onto stack
		PUSH_32,
		PUSH_R,

		POP,            // Pops value from stack
		POP_R,          // POP A - pops from stack into register A

		PRINT,          // Peeks and prints top most value on stack // temp
		PRINT_STR,      // Peeks and prints string pointer from stack // temp

		TOSTRING, // temp
		TOSTRING_STR, // temp

		NCALL, // Native call
		CALL_64,        // CALL A, B - calls A with B args
		CALL_R,

		RET,

		NONPRIV,        // Drops to non privileged execution level and jumps to I64
		PRIVTEST,
	}

	public unsafe class CPU {
		byte[] Memory;

		public bool Debug;
		public bool Halted;
		public Registers Regs;
		public ObjectStack Stack;
		public Dictionary<byte, object> IntHandlers;
		public List<Instruction> PrivilegedInstructions;

		public event Action<byte> OnInterrupt;

		public CPU(byte[] Mem) {
			Memory = Mem;
			Reset();
			PrivilegedInstructions = new List<Instruction>() {
				Instruction.PRIVTEST,
				Instruction.SHDLR,
				Instruction.NCALL,
			};
		}

		public void Reset() {
			Halted = false;
			Regs = new Registers();
			Stack = new ObjectStack();
			IntHandlers = new Dictionary<byte, object>();
		}

		public byte FetchInstrInt8() {
			byte B = Memory[Regs.IP++];
			return B;
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

		public ulong FetchInstrReg() {
			return (ulong)Regs.GP[FetchInstrInt8()];
		}

		public string FetchInstrString() {
			ulong Addr = (ulong)FetchInstrInt64();
			return FetchInstrString(Addr);
		}

		public string FetchInstrString(ulong Addr) {
			int Len = BitConverter.ToInt32(new[] { Memory[Addr], Memory[Addr + 1], Memory[Addr + 2], Memory[Addr + 3] }, 0);

			byte[] Bytes = new byte[Len];
			for (int i = 0; i < Len; i++)
				Bytes[i] = Memory[4 + (ulong)i + Addr];
			return Encoding.UTF8.GetString(Bytes);
		}

		public void Interrupt(byte Num) {
			bool OldPriv = Regs.Privileged;
			Regs.Privileged = true;

			if (OnInterrupt != null)
				OnInterrupt(Num);

			if (IntHandlers.ContainsKey(Num)) {
				object Handler = IntHandlers[Num];
				if (Handler is ulong) {
					ulong CD = Regs.CD;
					Call((ulong)Handler);
					while (Regs.CD != CD) {
						//Console.Write("::");
						Step();
					}
				}
			}

			Regs.Privileged = OldPriv;

			Halted = false;
		}

		public ulong Jump(ulong Addr, bool Bool = true) {
			ulong Cur = Regs.IP;
			if (Bool)
				Regs.IP = Addr;
			return Cur;
		}

		public void Call(ulong Addr) {
			Regs.CD++;
			Stack.Push(Jump(Addr));
		}

		public void Compare(long A, long B) {
			Regs.Lesser = A < B;
			Regs.Greater = A > B;
			Regs.Equal = A == B;
		}

		public Instruction Step() {
			if (Halted)
				return Instruction.NOP;
			Instruction I = (Instruction)FetchInstrInt8();
			if (Debug)
				Console.WriteLine(": {0}", I);

			if (!Regs.Privileged && PrivilegedInstructions.Contains(I))
				throw new Exception("Tried to execute privileged instruction " + I);

			switch (I) {
				case Instruction.PRIVTEST:
				case Instruction.NOP:
					break;
				case Instruction.HALT:
					Halted = true;
					break;
				case Instruction.RESET:
					Reset();
					break;

				case Instruction.INT_8:
					Interrupt(FetchInstrInt8());
					break;
				case Instruction.SHDLR: {
						byte B = FetchInstrInt8();
						if (IntHandlers.ContainsKey(B))
							IntHandlers.Remove(B);
						IntHandlers.Add(B, (ulong)FetchInstrInt64());
						break;
					}

				case Instruction.CMP_R_R:
					Compare((long)FetchInstrReg(), (long)FetchInstrReg());
					break;
				case Instruction.CMP_R_64:
					Compare((long)FetchInstrReg(), FetchInstrInt64());
					break;

				case Instruction.JMP_64:
					Jump((ulong)FetchInstrInt64());
					break;
				case Instruction.JMP_R:
					Jump(FetchInstrReg());
					break;
				case Instruction.JEQ_64:
					Jump((ulong)FetchInstrInt64(), Regs.Equal);
					break;
				case Instruction.JNE_64:
					Jump((ulong)FetchInstrInt64(), !Regs.Equal);
					break;
				case Instruction.JLE_64:
					Jump((ulong)FetchInstrInt64(), Regs.Lesser || Regs.Equal);
					break;
				case Instruction.JGE_64:
					Jump((ulong)FetchInstrInt64(), Regs.Greater || Regs.Equal);
					break;
				case Instruction.JL_64:
					Jump((ulong)FetchInstrInt64(), Regs.Lesser);
					break;
				case Instruction.JG_64:
					Jump((ulong)FetchInstrInt64(), Regs.Greater);
					break;

				case Instruction.ADD_R_R: {
						byte A = FetchInstrInt8();
						byte B = FetchInstrInt8();
						Regs.GP[A] += Regs.GP[B];
						break;
					}
				case Instruction.ADD_R_64: {
						byte A = FetchInstrInt8();
						Regs.GP[A] += FetchInstrInt64();
						break;
					}
				case Instruction.SUB_R_R: {
						byte A = FetchInstrInt8();
						byte B = FetchInstrInt8();
						Regs.GP[A] -= Regs.GP[B];
						break;
					}
				case Instruction.SUB_R_64: {
						byte A = FetchInstrInt8();
						Regs.GP[A] -= FetchInstrInt64();
						break;
					}

				case Instruction.MOV_R_64:
					Regs.GP[FetchInstrInt8()] = FetchInstrInt64();
					break;
				case Instruction.MOV_R_R:
					Regs.GP[FetchInstrInt8()] = (long)FetchInstrReg();
					break;

				case Instruction.PUSH_64:
					Stack.Push(FetchInstrInt64());
					break;
				case Instruction.PUSH_32:
					Stack.Push(FetchInstrInt32());
					break;
				case Instruction.PUSH_R:
					Stack.Push(FetchInstrReg());
					break;

				case Instruction.POP:
					Stack.Pop();
					break;
				case Instruction.POP_R:
					Regs.GP[FetchInstrInt8()] = ChangeType<long>(Stack.Pop());
					break;

				/*case Instruction.PINVOKE: {
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
					}*/

				case Instruction.PRINT:
					Console.Write(Stack.Peek());
					break;
				case Instruction.PRINT_STR:
					Console.Write(FetchInstrString(ChangeType<ulong>(Stack.Peek())));
					break;

				case Instruction.TOSTRING:
					Stack.Push(Stack.Pop().ToString());
					break;
				case Instruction.TOSTRING_STR:
					Stack.Push(FetchInstrString(ChangeType<ulong>(Stack.Pop())));
					break;

				case Instruction.NCALL: {
						string ImportFunc = FetchInstrString(ChangeType<ulong>(Stack.Pop()));
						string AsmName = "";
						if (ImportFunc.Contains(",")) {
							AsmName = ", " + ImportFunc.Substring(ImportFunc.IndexOf(',') + 1).Trim();
							ImportFunc = ImportFunc.Substring(0, ImportFunc.IndexOf(','));
						}

						string[] Namespaces = ImportFunc.Split('.');
						string ContainerClass = string.Join(".", Namespaces, 0, Namespaces.Length - 1);
						Type T = Type.GetType(ContainerClass + AsmName);
						MethodInfo MI = T.GetMethod(Namespaces[Namespaces.Length - 1], BindingFlags.Public | BindingFlags.Static);
						ParameterInfo[] Params = MI.GetParameters();
						object[] Args = new object[Params.Length];
						for (int i = Args.Length - 1; i >= 0; i--)
							Args[i] = ChangeType(Stack.Pop(), Params[i].ParameterType);
						MI.Invoke(null, Args);
						break;
					}
				case Instruction.CALL_64:
					Call((ulong)FetchInstrInt64());
					break;
				case Instruction.CALL_R:
					Call((ulong)FetchInstrReg());
					break;

				case Instruction.RET: {
						Regs.CD--;
						object Val = Stack.Pop();
						Jump(ChangeType<ulong>(Val));
						break;
					}

				case Instruction.NONPRIV:
					Regs.Privileged = false;
					break;

				default:
					throw new Exception("Invalid instruction " + I);
			}

			return I;
		}

		public T ChangeType<T>(object O) {
			return (T)ChangeType(O, typeof(T));
		}

		public object ChangeType(object O, Type T) {
			if (O is Int64 && T == typeof(IntPtr))
				return new IntPtr((Int64)O);
			return Convert.ChangeType(O, T);
		}
	}
}