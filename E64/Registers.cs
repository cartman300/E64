using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace E64 {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Registers {
		public UInt64[] IV;
		public Int64[] GP;
		public UInt64 IP, CR;
		public Indexable<int, bool> CRFlags;

		public Registers(int GPLen = 16) {
			GP = new Int64[GPLen];
			IV = new UInt64[256];

			CRFlags = new Indexable<int, bool>((K) => {
				return CR.GetBit(K);
			}, (K, V) => {
				CR = CR.SetBit(K, V);
			}, (K) => {
				if (K < 0 || K >= sizeof(UInt64) * 8)
					throw new CPUException("Invalid CRFlags bit range: {0}", K);
			});
		}
	}
}