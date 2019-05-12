using System;
using System.IO;
using System.Linq;
using fNbt;
using log4net;
using MiNET.LevelDB;
using MiNET.LevelDB.Utils;
using NUnit.Framework;

namespace MiNET.LevelDBTests
{
	[TestFixture]
	public class BedrockParsingTests
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BedrockParsingTests));


		[Test]
		public void LevelDbGetValueFromKey()
		{
			var db = new Database(new DirectoryInfo("My World.mcworld"));
			db.Open();

			int x = 0;
			int z = 0;
			byte y = 0;

			var versionKey = BitConverter.GetBytes(x).Concat(BitConverter.GetBytes(z)).Concat(new byte[] {0x76}).ToArray();
			var version = db.Get(versionKey);
			Assert.AreEqual(10, version.First());

			var chunkDataKey = BitConverter.GetBytes(x).Concat(BitConverter.GetBytes(z)).Concat(new byte[] {0x2f, y}).ToArray();
			var result = db.Get(chunkDataKey);

			Assert.AreEqual(new byte[] {0x08, 0x01, 0x08, 0x00, 0x11}, result.AsSpan(0, 5).ToArray());

			ParseChunk(result);
		}

		private void ParseChunk(ReadOnlySpan<byte> data)
		{
			var reader = new SpanReader(data);

			var version = reader.ReadByte();
			Assert.AreEqual(8, version); // new palette-based chunk format

			var storageSize = reader.ReadByte();
			for (int i = 0; i < storageSize; i++)
			{
				var bitsPerBlock = reader.ReadByte() >> 1;
				Assert.AreEqual(4, bitsPerBlock);
				int numberOfBytes = 4096/(32/bitsPerBlock)*4;
				var blockData = reader.Read(numberOfBytes);

				Assert.AreEqual(4096/2, blockData.Length);

				int paletteSize = reader.ReadInt32();
				Assert.AreEqual(12, paletteSize);

				for (int j = 0; j < paletteSize; j++)
				{
					NbtFile file = new NbtFile();
					file.BigEndian = false;
					file.UseVarInt = false;
					var buffer = data.Slice(reader.Position).ToArray();

					int numberOfBytesRead = (int) file.LoadFromStream(new MemoryStream(buffer), NbtCompression.None);
					reader.Position += numberOfBytesRead;
					Console.WriteLine(file.RootTag);
					Assert.NotZero(numberOfBytesRead);
				}
			}
		}
	}
}