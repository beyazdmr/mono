//
// XslAvt.cs
//
// Author:
//	Ben Maurer (bmaurer@users.sourceforge.net)
//
// (C) 2003 Ben Maurer
//

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Mono.Xml.Xsl.Operations {
	// Represents an Attribute Value Template in XSL.
	internal class XslAvt {
		
		string simpleString;
		ArrayList avtParts;
		
		public XslAvt (string str, Compiler comp) {
			if (str.IndexOf ("{") == -1 && str.IndexOf ("}") == -1) {
				// That was easy ;-).
				simpleString = str;
				return;
			}
			avtParts = new ArrayList ();
			StringBuilder sb = new StringBuilder ();
			StringReader r = new StringReader (str);
			
			
			
			while (r.Peek () != -1) {
				char c = (char)r.Read ();
				switch (c) {					
				case '{':
					if ((char)r.Peek () == '{') {
						// {{ == escaped {
						sb.Append ((char)r.Read ());
						break;
					}
					
					if (sb.Length != 0) {
						// Ok, we have already found a text
						// part, lets save that.
						avtParts.Add (new SimpleAvtPart (sb.ToString ()));
						sb.Length = 0;
					}
					
					while ((c = (char)r.Read ()) != '}') {
						switch (c) {
						case '\'': case '"': {
							// We are inside a quote
							char unq = c;
							sb.Append (c);
							while ((c = (char)r.Read ()) != unq) {
								sb.Append (c);
								if (r.Peek () == -1)
									throw new XsltCompileException ("unexpected end of AVT", null, comp.Input);
							}
								
							
							sb.Append (c);
							break;
						} // ' or "
						default:
							sb.Append (c);
							break;
						}
						if (r.Peek () == -1) throw new XsltCompileException ("unexpected end of AVT", null, comp.Input);
					}
					
						
					avtParts.Add (new XPathAvtPart (comp.CompileExpression (sb.ToString ())));
					sb.Length = 0;
					break;
				case '}':
					c = (char)r.Read ();
					if (c != '}')
						throw new XsltCompileException ("Braces must be escaped", null, comp.Input);
					goto default;
				default:
					sb.Append (c);
					break;
				}
			}
			if (sb.Length != 0) {
				// Ok, we have already found a text
				// part, lets save that.
				avtParts.Add (new SimpleAvtPart (sb.ToString ()));
				sb.Length = 0;
			}
		}
		
		public static string AttemptPreCalc (ref XslAvt avt)
		{
			if (avt == null) return null;
			if (avt.simpleString != null) {
				string s = avt.simpleString;
				avt = null;
				return s;
			}
			return null;
		}
		
		public string Evaluate (XslTransformProcessor p)
		{
			if (simpleString != null) return simpleString;
			if (avtParts.Count == 1)
				return ((AvtPart) avtParts [0]).Evaluate (p);
				
			StringBuilder sb = p.GetAvtStringBuilder ();
			
			int len = avtParts.Count;
			for (int i = 0; i < len; i++)
				sb.Append (((AvtPart) avtParts [i]).Evaluate (p));
			
			return p.ReleaseAvtStringBuilder ();
		}
		
		// Represents part of an AVT
		abstract class AvtPart {
			public abstract string Evaluate (XslTransformProcessor p);
		}
		
		sealed class SimpleAvtPart : AvtPart {
			string val;
			public SimpleAvtPart (string val)
			{
				this.val = val;
			}
			
			public override string Evaluate (XslTransformProcessor p)
			{
				return val;
			}
		}
		
		sealed class XPathAvtPart : AvtPart {
			XPathExpression expr;
			
			public XPathAvtPart (XPathExpression expr)
			{
				this.expr = expr;
			}
			
			public override string Evaluate (XslTransformProcessor p)
			{
				return p.EvaluateString (expr);
			}
		}
	}
}