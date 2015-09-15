using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E64 {
	class CPUException : Exception {
		public CPUException(string Msg)
			: base(Msg) {
		}

		public CPUException(string Fmt, params object[] Args)
			: this(string.Format(Fmt, Args)) {
		}
	}
}