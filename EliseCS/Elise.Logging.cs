using System;
using System.IO;
using System.Text;

namespace Elise.Logging
{
	public static class Logger
	{
		static readonly UTF8Encoding encoding = new UTF8Encoding(false);

		static Logger()
		{
			File.WriteAllText("out.log", string.Empty, encoding);
		}

		public static void Log(params object[] outputs)
		{
			File.AppendAllText("out.log", string.Join("\n", outputs), encoding);
		}
	}
}
