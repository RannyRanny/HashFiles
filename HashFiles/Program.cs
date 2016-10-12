using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;
using SQLite;


namespace HashFiles
{
	class Program
	{
		private static Queue<string> files = new Queue<string>();
		private const string hashBase = "HashBase.sqlite";
		private static int fileMaxCount;
		private static int fileCount = 0;
		private static List<FileHashInfo> hashes = new List<FileHashInfo>();
		private static ManualResetEvent endEvent = new ManualResetEvent(false);

		static void Main(string[] args)
		{
			if (!File.Exists(hashBase))
			{
				var db = new SQLiteConnection(hashBase);									//создание БД если ее не существует
				db.CreateTable<FileHashInfo>();
				db.Close();
			}
			string path;
			Console.WriteLine("Введите путь для хеширования файлов");
			path = Console.ReadLine();
			if (!Directory.Exists(path))
			{
				Console.WriteLine("Такой папки не существует");
				Console.ReadKey();
				return;
			}
			DirectoryInfo di = new DirectoryInfo(path);
			fileMaxCount = di.GetFiles().Length;

			Task getFiles = new Task(new Action(() => GetFiles(path)));
			Task getHashes = new Task(new Action(() => RunHash()));

			getFiles.Start();
			Thread.Sleep(100);              //ожидание пока в очереди появятся файлы
			RunHash();
			endEvent.WaitOne();

			var dbWriter = new SQLiteConnection(hashBase);
			if (!AddHashToDB(dbWriter, hashes)) return;                                       //запись полученных данных в базу данных
			dbWriter.Close();
			Console.WriteLine();
			Console.WriteLine("Запись в базу успешно произведена");
			Console.ReadLine();
		}

		/// <summary>
		/// Отправка файлов в очередь
		/// </summary>
		/// <param name="path"></param>
		private static void GetFiles(string path)
		{
			foreach (string file in Directory.GetFiles(path))
			{
				lock (files)
				{
					files.Enqueue(file);
				}
			}
		}

		/// <summary>
		/// Метод расчета хеша файла
		/// </summary>
		/// <param name="filePath">Путь к файлу</param>
		private static void GetHash(object filePath)
		{
			MD5 md5 = MD5.Create();
			byte[] data = md5.ComputeHash(new FileStream((string)filePath, FileMode.Open));

			StringBuilder sBuilder = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}
			hashes.Add(new FileHashInfo()
			{
				Hash = sBuilder.ToString(),
				PathFile = (string)filePath
			});
			fileCount++;
			Console.Write("\r {0} файлов обработано из {1} ", fileCount, fileMaxCount);
			if (fileCount == fileMaxCount) endEvent.Set();
		}

		/// <summary>
		/// Получение файла из очереди и запуск расчета хеша
		/// </summary>
		private static void RunHash()
		{
			string file = "";
			lock (files)
			{
				if (files.Count > 0)
				{
					file = files.Dequeue();
				}
				else return;
			}
			Task getHash = new Task(new Action(() => GetHash(file)));
			getHash.Start();
			RunHash();
		}

		private static bool AddHashToDB(SQLite.SQLiteConnection db, List<FileHashInfo> hashes)
		{
			try
			{
				var s = db.InsertAll(hashes);
			}
			catch
			{
				Console.WriteLine();
				Console.WriteLine("Ошибка базы данных");
				Console.ReadLine();
				return false;
			}
			return true;
		}

		/// <summary>
		/// Класс для формирования базы данных
		/// </summary>
		class FileHashInfo
		{
			[PrimaryKey, AutoIncrement, Unique]
			public int ID { get; set; }

			[MaxLength(200)]
			public string PathFile { get; set; }

			[MaxLength(200)]
			public string Hash { get; set; }
		}
	}
}
