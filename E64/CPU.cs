﻿using System;
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
		LOCK,
		UNLOCK,
		INT_I8,
		INT_REG,
		JUMP_I64,
		JUMP_REG,
		MOVE_REG_I64,
		MOVE_REG_REG,
		PRINT_REG,
	}

	public unsafe class CPU {
		public bool Halted;
		Indexable<UInt64, byte> Memory;

		public event Action<byte> OnInterrupt;
		public Registers Regs;

		public CPU(Indexable<UInt64, byte> Mem) {
			Halted = false;
			Regs = new Registers();
			Memory = Mem;
		}

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
				case Instruction.LOCK:
					Monitor.Enter(Memory);
					break;
				case Instruction.UNLOCK:
					Monitor.Exit(Memory);
					break;
				case Instruction.INT_I8:
					Interrupt(FetchInstrInt8());
					break;
				case Instruction.INT_REG:
					Interrupt((byte)(Regs.GP[FetchInstrInt8()] & 0xFF));
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
				case Instruction.PRINT_REG: {
						byte Num = FetchInstrInt8();
						Console.WriteLine("GP[{0}] = {1}", Num, Regs.GP[Num]);
					}
					break;
				default:
					throw new Exception("Invalid instruction " + I);
			}
		}
	}
}