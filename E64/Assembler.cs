using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace E64 {
	public class Assembler {
		struct _Label {
			public string Name;

			public _Label(string Name) {
				this.Name = Name;
			}
		}

		struct _AddressOf {
			public string Name;

			public _AddressOf(string Name) {
				this.Name = Name;
			}
		}

		List<object> TextObjects;
		List<object> DataObjects;
		int DtaCnt;

		public Assembler() {
			TextObjects = new List<object>();
			DataObjects = new List<object>();
			DtaCnt = 0;
		}

		public Assembler Instr(Instruction I) {
			return Raw((byte)I);
		}

		public Assembler Reg(byte B) {
			return Raw(B);
		}

		public Assembler Int8(byte B) {
			return Raw(B);
		}

		public Assembler Int16(Int16 I) {
			return Raw(I);
		}

		public Assembler Int32(Int32 I) {
			return Raw(I);
		}

		public Assembler Int64(Int64 I) {
			return Raw(I);
		}

		public Assembler Label(string Name) {
			return Raw(new _Label(Name));
		}

		public Assembler AddressOf(string Name) {
			return Raw(new _AddressOf(Name));
		}
		
		public Assembler Raw(object O) {
			TextObjects.Add(O);
			return this;
		}

		public Assembler Data(string Name, object Val) {
			AddressOf(Name);
			DataObjects.Add(new _Label(Name));
			DataObjects.Add(Val);
			return this;
		}

		public Assembler Data(object Val) {
			for (int i = 0; i < DataObjects.Count; i++)
				if (DataObjects[i] == Val) {
					AddressOf(((_Label)DataObjects[i - 1]).Name);
					return this;
				}

			return Data("DTA_" + DtaCnt++, Val);
		}

		public byte[] ToByteArray() {
			List<byte> Bytes = new List<byte>();
			List<object> OldText = new List<object>(TextObjects);
			TextObjects.AddRange(DataObjects);

			for (int i = 0; i < TextObjects.Count; i++)
				Bytes.AddRange(GetBytes(TextObjects[i]));

			TextObjects.Clear();
			TextObjects.AddRange(OldText);
			return Bytes.ToArray();
		}

		byte[] GetBytes(object Obj) {
			if (Obj is _AddressOf) {
				_AddressOf Addr = (_AddressOf)Obj;
				long AddrOf = GetAddressOf(Addr.Name);
				return GetBytes(AddrOf);
			} else if (Obj is byte)
				return new byte[] { (byte)Obj };
			else if (Obj is string) {
				return GetBytes(((string)Obj).Length).Append(Encoding.UTF8.GetBytes((string)Obj));
			}

			if (SizeOf(Obj) == 0)
				return new byte[] { };


			Type T = Obj.GetType();
			return (byte[])typeof(BitConverter).GetMethod("GetBytes", new Type[] { T }).Invoke(null, new object[] { Obj });
		}

		int GetAddressOf(string Name) {
			for (int i = 0; i < TextObjects.Count; i++)
				if (TextObjects[i] is _Label && ((_Label)TextObjects[i]).Name == Name)
					return GetAddressOf(i);
			return 0;
		}

		int GetAddressOf(int Idx) {
			int Offset = 0;

			for (int i = 0; i < Idx; i++)
				Offset += SizeOf(TextObjects[i]);

			return Offset;
		}

		int SizeOf(object Obj) {
			if (Obj is _Label)
				return 0;
			else if (Obj is _AddressOf)
				return sizeof(long);
			else if (Obj is string)
				return GetBytes((string)Obj).Length;

			return Marshal.SizeOf(Obj.GetType());
		}
	}
}
