using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E64 {
	public class Stack<T> : List<T> {
		public void Push(T Val) {
			Add(Val);
		}

		public T Pop(int Idx = -1) {
			T Val = Peek(Idx);
			RemoveAt(GetIdx(Idx));
			return Val;
		}

		public TT Pop<TT>(int Idx = -1) {
			return (TT)Convert.ChangeType(Pop(Idx), typeof(TT));
		}

		public T Peek(int Idx = -1) {
			return this[GetIdx(Idx)];
		}

		public void SetCount(int Cnt) {
			while (Count > Cnt)
				Pop();
		}

		int GetIdx(int Idx) {
			if (Idx < 0)
				return Count + Idx;
			return Idx;
		}
	}
}