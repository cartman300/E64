using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace E64 {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Registers {
		public const int PRIV = 0;
		public const int EQUL = 1;
		public const int LSSR = 2;
		public const int GRTR = 3;
		public const int INTF = 4;

		public bool Privileged
		{
			get { return CRFlags[PRIV]; }
			set { CRFlags[PRIV] = value; }
		}

		public bool Equal
		{
			get { return CRFlags[EQUL]; }
			set { CRFlags[EQUL] = value; }
		}

		public bool Lesser
		{
			get { return CRFlags[LSSR]; }
			set { CRFlags[LSSR] = value; }
		}

		public bool Greater
		{
			get { return CRFlags[GRTR]; }
			set { CRFlags[GRTR] = value; }
		}
		
		public bool InterruptsEnabled
		{
			get { return CRFlags[INTF]; }
			set { CRFlags[INTF] = value; }
		}

		public long[] GP;   // General purpose
		public ulong IP;    // Instruction pointer
		public ulong CR;    // Control register
		public ulong CD;    // Call depth register
		public Indexable<int, bool> CRFlags;

		public Registers(int GPLen = 64, bool StartPrivileged = true) {
			GP = new Int64[GPLen];

			CRFlags = new Indexable<int, bool>((K) => {
				return CR.GetBit(K);
			}, (K, V) => {
				CR = CR.SetBit(K, V);
			}, (K) => {
				if (K < 0 || K >= sizeof(UInt64) * 8)
					throw new CPUException("Invalid CRFlags bit range: {0}", K);
			});

			Privileged = StartPrivileged;
		}
	}
}