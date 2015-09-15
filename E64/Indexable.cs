using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E64 {
	public class Indexable<K, V> {
		Func<K, V> GetFunc;
		Action<K, V> SetFunc;
		Action<K> RangeCheckFunc;

		public Indexable(Func<K, V> Get, Action<K, V> Set, Action<K> RangeCheck = null) {
			GetFunc = Get;
			SetFunc = Set;
			RangeCheckFunc = RangeCheck;
		}

		public V this[K Key] {
			get {
				if (RangeCheckFunc != null)
					RangeCheckFunc(Key);
				return GetFunc(Key);
			}
			set {
				if (RangeCheckFunc != null)
					RangeCheckFunc(Key);
				SetFunc(Key, value);
			}
		}
	}
}