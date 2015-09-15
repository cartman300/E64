using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E64 {
	static class Utils {
		public static UInt64 SetBit(this UInt64 Num, int Bit, bool Val) {
			if (Val) {
				Num |= 1u << Bit;
			} else {
				Num &= ~(1u << Bit);
			}
			return Num;
		}

		public static bool GetBit(this UInt64 Num, int Bit) {
			return ((Num >> Bit) & 1) == 1;
		}
	}
}