using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;

namespace HashFiles
{
	class Program
	{
		private static Queue<string> files = new Queue<string>();
		static void Main(string[] args)
		{
			string path = "C:\\Qt\\";
			//Console.WriteLine("Введите путь для хеширования файлов");
			//path=Console.ReadLine();
			//if (!Directory.Exists(path))
			//{
			//	Console.WriteLine("No such directory");
			//	return;
			//}
			Task getFiles = new Task(new Action(() => GetFiles(path)));
			Task getHashes = new Task(new Action(() => RunHash()));
			getFiles.Start();
			Thread.Sleep(1000);
			getHashes.Start();
			getHashes.Wait();
			Console.ReadLine();
		}

		private static void GetFiles(string path)
		{
			foreach (string file in Directory.GetFiles(path))
			{
				lock (files)
				{
					files.Enqueue(file);
					Monitor.PulseAll(files);
				}

			}
		}

		private static void GetHash(string filePath)
		{
			MD5 md5 = MD5.Create();
			byte[] data = md5.ComputeHash(new FileStream(filePath, FileMode.Open));

			StringBuilder sBuilder = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}
			Console.WriteLine(sBuilder.ToString()+"   "+filePath);
		}

		private static void RunHash()
		{
			lock (files)
			{
				if (files.Count > 0)
				{
					string file = files.Dequeue();
					Monitor.PulseAll(files);
					Task.Run((new Action(() => RunHash())));
					GetHash(file);
				}
			}
		}
	}
}
