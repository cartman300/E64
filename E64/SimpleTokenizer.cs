using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E64 {
	static class SimpleTokenizer {
		static void PushTokens(List<string> Tokens, StringBuilder Tmp) {
			if (Tmp.Length > 0) {
				Tokens.Add(Tmp.ToString());
				Tmp.Clear();
			}
		}

		public static string[] Tokenize(string Input, char? CommentStart = null, char[] Symbols = null) {
			List<string> Tokens = new List<string>();
			StringBuilder Tmp = new StringBuilder();
			bool InQuote = false;
			bool InComment = false;

			for (int i = 0; i < Input.Length; i++) {
				if (CommentStart.HasValue) {
					if (Input[i] == CommentStart)
						InComment = true;
					else if (Input[i] == '\n')
						InComment = false;
					if (InComment)
						continue;
				}

				if (Input[i] == '\"') {
					if (InQuote = !InQuote) {
						PushTokens(Tokens, Tmp);
						Tmp.Append(Input[i]);
					} else {
						Tmp.Append(Input[i]);
						Tmp.Replace("\\n", "\n");
						Tmp.Replace("\\t", "\t");
						PushTokens(Tokens, Tmp);
					}
					continue;
				}

				if (InQuote) {
					Tmp.Append(Input[i]);
					continue;
				}

				if (Symbols != null && Symbols.Contains(Input[i])) {
					PushTokens(Tokens, Tmp);
					Tmp.Append(Input[i]);
					PushTokens(Tokens, Tmp);
					continue;
				}

				if (char.IsWhiteSpace(Input[i]))
					PushTokens(Tokens, Tmp);
				else
					Tmp.Append(Input[i]);
			}
			PushTokens(Tokens, Tmp);

			return Tokens.ToArray();
		}
	}
}
