using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using E64Stack = E64.Stack<ulong>;

namespace E64 {
	public delegate ulong PortFunc(ulong Data, bool IsRead);

	// _R _8 _32 _64
	public enum Instruction : byte {
		NOP = 1,
		HALT,
		RESET,
		BREAKPOINT,

		INT_8, // INT A - interrupts with A
		SHDLR, // SIH A B - set interrupt handler B for interrupt A

		CLI,
		STI,

		CMP_R_R, // COMPARE A B - compares A and B and sets the flags register
		CMP_R_64,

		JMP_64, // JUMP A - jumps to A
		JMP_R,
		JEQ_64, // JEQ A - jumps if equal to A
		JNE_64, // JNE A - jumps if not equal to A
		JLE_64, // JLE A - jumps if less than or equal to A 
		JGE_64, // JGE A - jumps if greater or equal to A
		JL_64,  // JLE A - jumps if less than or equal to A 
		JG_64,  // JGE A - jumps if greater or equal to A

		ADD_R_R, // ADD A B - adds A and B and stores in A
		ADD_R_64,
		SUB_R_R, // DEC A B - adds A and B and stores in A
		SUB_R_64,

		MOV_R_64, // MOVE A B - moves B into A
		MOV_R_R,

		READ8_R_R, // READ A B - reads memory B into A
		READ32_R_R,

		OUT_64_64, // OUT A B - writes B into port A
		OUT_64_R,
		IN_R_64, // IN A B - reads from port B into registar A

		PUSH_64, // PUSH A - pushes A onto stack
		PUSH_R,

		POP, // Pops value from stack
		POP_R, // Pops from stack into register

		CALL_64, // CALL A, B - calls A with B args
		CALL_R,

		RET,

		NONPRIV, // Drops to non privileged execution level and jumps to I64
		PRIVTEST, // TEMP
		PRINT_PRIV,
	}

	public unsafe class CPU {
		byte[] Memory;

		public bool Debug;
		public bool Halted;
		public Registers Regs;
		public E64Stack Stack;
		public List<Instruction> PrivilegedInstructions;
		public Dictionary<byte, ulong> IntHandlers;
		public Dictionary<ulong, PortFunc> Ports;

		public event Action<byte> OnInterrupt;

		public CPU(byte[] Mem) {
			Memory = Mem;
			Reset();

			PrivilegedInstructions = new List<Instruction>() {
				Instruction.PRIVTEST,
				Instruction.SHDLR,
				Instruction.OUT_64_64,
				Instruction.OUT_64_R,
				Instruction.IN_R_64,
			};
			Ports = new Dictionary<ulong, PortFunc>();
		}

		public void Reset() {
			Regs = new Registers();
			Stack = new E64Stack();
			IntHandlers = new Dictionary<byte, ulong>();

			Halted = false;
			Regs.InterruptsEnabled = true;
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
			return FetchString(Addr);
		}

		public byte FetchInt8(ulong Addr) {
			byte B = Memory[Addr];
			return B;
		}

		public long FetchInt64(ulong Addr) {
			byte[] Bytes = new[] { FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(),
				FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8(), FetchInstrInt8() };
			return BitConverter.ToInt64(Bytes, 0);
		}

		public void StoreInt8(ulong Addr, byte B) {
			Memory[Addr] = B;
		}

		public void StoreInt64(ulong Addr, long L) {
			byte[] Bytes = BitConverter.GetBytes(L);
			for (int i = 0; i < Bytes.Length; i++)
				StoreInt8(Addr + (ulong)i, Bytes[i]);
		}

		public string FetchString(ulong Addr) {
			int Len = BitConverter.ToInt32(new[] { Memory[Addr], Memory[Addr + 1], Memory[Addr + 2], Memory[Addr + 3] }, 0);

			byte[] Bytes = new byte[Len];
			for (int i = 0; i < Len; i++)
				Bytes[i] = Memory[4 + (ulong)i + Addr];
			return Encoding.UTF8.GetString(Bytes);
		}

		public void Interrupt(byte Num) {
			if (!Regs.InterruptsEnabled)
				return;
			Halted = false;
			Regs.Privileged = true;

			if (OnInterrupt != null)
				OnInterrupt(Num);

			if (IntHandlers.ContainsKey(Num))
				Call(IntHandlers[Num]);
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

		public void Return() {
			Regs.CD--;
			Jump(Stack.Pop());
		}

		public void Compare(long A, long B) {
			Regs.Lesser = A < B;
			Regs.Greater = A > B;
			Regs.Equal = A == B;
		}

		public void Out(ulong Port, ulong Data) {
			if (Ports.ContainsKey(Port))
				Ports[Port](Data, false);
		}

		public ulong In(ulong Port) {
			ulong Data = 0;
			if (Ports.ContainsKey(Port))
				Data = Ports[Port](0, true);
			return Data;
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
				case Instruction.BREAKPOINT:
					Debugger.Break();
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

				case Instruction.CLI:
					Regs.InterruptsEnabled = false;
					break;
				case Instruction.STI:
					Regs.InterruptsEnabled = true;
					break;

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

				case Instruction.READ8_R_R: {
						Regs.GP[FetchInstrInt8()] = Memory[FetchInstrReg()];
						break;
					}
				case Instruction.READ32_R_R: {
						byte Dest = FetchInstrInt8();
						ulong Loc = FetchInstrReg();
						Regs.GP[Dest] = BitConverter.ToInt32(new byte[] {
							Memory[Loc], Memory[Loc+1], Memory[Loc+2], Memory[Loc+3]
						}, 0);
						break;
					}

				case Instruction.OUT_64_64:
					Out((ulong)FetchInstrInt64(), (ulong)FetchInstrInt64());
					break;
				case Instruction.OUT_64_R:
					Out((ulong)FetchInstrInt64(), FetchInstrReg());
					break;
				case Instruction.IN_R_64: {
						byte Reg = FetchInstrInt8();
						Regs.GP[Reg] = (long)In((ulong)FetchInstrInt64());
						break;
					}

				case Instruction.PUSH_64:
					Stack.Push((ulong)FetchInstrInt64());
					break;
				case Instruction.PUSH_R:
					Stack.Push(FetchInstrReg());
					break;

				case Instruction.POP:
					Stack.Pop();
					break;
				case Instruction.POP_R:
					Regs.GP[FetchInstrInt8()] = (long)Stack.Pop();
					break;

				case Instruction.CALL_64:
					Call((ulong)FetchInstrInt64());
					break;
				case Instruction.CALL_R:
					Call((ulong)FetchInstrReg());
					break;

				case Instruction.RET:
					Return();
					break;

				case Instruction.NONPRIV:
					Regs.Privileged = false;
					break;

				case Instruction.PRINT_PRIV:
					if (Regs.Privileged)
						Console.WriteLine("Privileged");
					else
						Console.WriteLine("Protected");
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