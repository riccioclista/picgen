//
// PictureGenerator.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2012 Antonius Riha
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PictureGen
{
	public class PictureGenerator
	{
		public PictureGenerator ()
		{
			rand = new Random ((int)DateTime.Now.Ticks);

			inputCfdgPaths = new List<string> ();
			foreach (var file in Directory.GetFiles ("input")) {
				if (!file.StartsWith ("i_"))
					inputCfdgPaths.Add (file);
			}
		}

		public byte [] Next ()
		{
			// pick an input cfdg
			var idx = rand.Next (inputCfdgPaths.Count - 1);
			var cfdgPath = inputCfdgPaths [idx];

			// Setup cfdg process info
			var psi = new ProcessStartInfo ("cfdg", "-s500 -");
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;
			psi.RedirectStandardOutput = true;
			var p = new Process ();
			p.StartInfo = psi;
			p.Start ();

			// parse input cfdg and generate target cfdg
			var fs = File.OpenRead (cfdgPath);
			var sr = new StreamReader (fs);
			var sw = p.StandardInput;
			int c;
			while ((c = sr.Read ()) != -1) {
				var character = (char)c;
				switch (character) {
				case '@':
					var expr = ReadExpression (sr);
					Debug.Write (expr);
					sw.Write (expr);
					break;
				default:
					Debug.Write (character);
					sw.Write (character);
					break;
				}
			}

			sr.Close ();
			fs.Close ();
			sw.Close ();

			List<byte> img = new List<byte> ();
			using (var br = new BinaryReader (p.StandardOutput.BaseStream)) {
				try {
					while (true)
						img.Add (br.ReadByte ());
				} catch (EndOfStreamException) {}
			}

			p.WaitForExit ();
			p.Close ();

			return img.ToArray ();
		}

		string ReadExpression (StreamReader sr)
		{
			// read next three chars to get type info
			char [] type = new char [3];
			sr.Read (type, 0, 3);

			// read delimiter (only for moving the cursor)
			sr.Read ();

			var typeStr = new string (type);
			if (typeStr == "int") {
				var lower = ReadInt (sr);
				var upper = ReadInt (sr);
				return GetShortString (rand.Next (lower, upper).ToString ());
			} else if (typeStr == "dbl") {
				var lower = ReadDouble (sr);
				var upper = ReadDouble (sr);
				var dbl = rand.NextDouble ();
				var n = dbl * (upper - lower) + lower;
				return GetShortString (n.ToString ());
			}

			throw new Exception ("Not parsable.");
		}

		int ReadInt (StreamReader sr)
		{
			var nStr = string.Empty;
			int c;
			while ((c = sr.Read ()) != -1) {
				var character = (char)c;
				switch (character) {
				case '|':
				case '@':
					return int.Parse (nStr);
				default:
					nStr += character;
					break;
				}
			}

			throw new Exception ("Not parsable.");
		}

		double ReadDouble (StreamReader sr)
		{
			var nStr = string.Empty;
			int c;
			while ((c = sr.Read ()) != -1) {
				var character = (char)c;
				switch (character) {
				case '|':
				case '@':
					return double.Parse (nStr);
				default:
					nStr += character;
					break;
				}
			}
			
			throw new Exception ("Not parsable.");
		}

		string GetShortString (string source)
		{
			var length = source.Length > 5 ? 5 : source.Length;
			return source.Substring (0, length);
		}

		public string Generate (string cfdgSourceFile)
		{
			var outFileName = Path.GetFileNameWithoutExtension (cfdgSourceFile) + ".png";
			var p = Process.Start ("cfdg", "-s500 " + cfdgSourceFile + " " + outFileName);
			p.WaitForExit ();
			return outFileName;
		}

		List<string> inputCfdgPaths;
		Random rand;
	}
}
