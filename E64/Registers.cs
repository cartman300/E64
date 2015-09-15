using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace E64 {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Registers {
		Int64[] GPInternal;

		public Indexable<int, Int64> GP;
		public UInt64 PC;
		public UInt64 CR;
		public Indexable<int, bool> CFlags;

		public Registers(int GPLen = 16) {
			GPInternal = new Int64[GPLen];

			GP = new Indexable<int, long>((K) => {
				return GPInternal[K];
			}, (K, V) => {
				GPInternal[K] = V;
			}, (K) => {
				if (K < 0 || K >= GPInternal.Length)
					throw new CPUException("GP register number out of range: {0}", GPInternal.Length);
			});

			CFlags = new Indexable<int, bool>((K) => {
				return CR.GetBit(K);
			}, (K, V) => {
				CR = CR.SetBit(K, V);
			}, (K) => {
				if (K < 0 || K >= sizeof(UInt64) * 8)
					throw new CPUException("Invalid CFlags bit range: {0}", K);
			});
		}
	}
}