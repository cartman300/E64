using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E64 {
	public class CPU {
		public Indexable<UInt64, byte> Memory;
		public Registers Regs;

		public CPU(Indexable<UInt64, byte> Mem) {
			Regs = new Registers();
			Memory = Mem;
		}

		public void Step() {

		}
	}
}