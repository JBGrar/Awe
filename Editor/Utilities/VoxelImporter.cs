using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AweEditor
{
	class VoxelImporter
	{
		const int CHUNK_DEFAULT_MAX = 1024 * 64;
		const int CHUNK_INFLATE_MAX = 1024 * 128;
		public bool GetRegion(string filename,int cx, int cz, byte[] blockData)
		{
			using (FileStream fs = File.Open(filename,FileMode.Open))
			{
				byte[] input, output;
				byte[] buff = new byte[5];

				int sectorNumber, offset, chunkLength;

				//input = new byte[CHUNK_DEFAULT_MAX];
				//output = new byte[CHUNK_DEFAULT_MAX];
				fs.Seek(4 * ((cx & 31) + (cz & 31) * 32), SeekOrigin.Begin);
				fs.Read(buff, 0, 4);

				sectorNumber = buff[3];
				offset = (buff[0] << 16) | (buff[1] << 8) | buff[2];

				if (offset == 0)
					return false;

				fs.Seek(4096 * offset, SeekOrigin.Begin);

				fs.Read(buff, 0, 5);

				chunkLength = (buff[0]<<24)|(buff[1]<<16)|(buff[2]<<8)|buff[3];
				
				if((chunkLength>sectorNumber*4096)||(chunkLength>CHUNK_DEFAULT_MAX))
					return false;
				input = new byte[chunkLength - 6];
				fs.Seek(2, SeekOrigin.Current);
				if(fs.Read(input,0,chunkLength-6)==0)
					return false;

				output = new byte[chunkLength];
				DeflateStream dfs = new DeflateStream(new MemoryStream(input),
													CompressionMode.Decompress);
				dfs.Flush();
				dfs.Read(output,0,input.Length);
				return GetBlocks(new MemoryStream(output),blockData);
			}
			
		}

		private bool GetBlocks(MemoryStream stream, byte[] buff)
		{
			int length, found, nsections;

			stream.Seek(1, SeekOrigin.Current);
			length = ReadWord(stream);
			stream.Seek(length, SeekOrigin.Current);

			if (FindElement(stream, "Level") != 10)
				return false;

			if(FindElement(stream, "Biomes")!=7)
				return false;

			//skip biome data
			stream.Seek(4,SeekOrigin.Current);
			stream.Seek(16*16,SeekOrigin.Current);

			if (FindElement(stream, "Sections") != 9)
				return false;
			{
				byte[] type = new byte[1];
				stream.Read(type, 0, 1);

				if (type[0] != 10)
					return false;
			}

			nsections = ReadDWord(stream);
			while ((nsections) > 0)
			{
				byte[] y = new byte[1];
				long save = stream.Position;

				if (FindElement(stream, "Y") != 1)
					return false;

				stream.Read(y, 0, 1);
				stream.Seek(save, SeekOrigin.Begin);

				found = 0;

				while (true)
				{
					bool ret = false;
					byte[] type = new byte[1];
					stream.Read(type, 0, 1);
					if (type[0] == 0)
						break;
					length = ReadWord(stream);

					byte[] name = new byte[length + 1];
					stream.Read(name, 0, length);
					name[length] = 0;
					char[] temp = new char[length + 1];
					for (int i = 0; i < temp.Length; i++)
					{
						temp[i] = (char)name[i];
					}

					string thisName = new string(temp);

					if (string.Compare(thisName, "BlockLight") == 0)
					{
						found++;
						ret = true;
						length = ReadDWord(stream);
						stream.Seek(length, SeekOrigin.Current);
					}
					if (string.Compare(thisName, "Blocks") == 0)
					{
						found++;
						ret = true;
						length = ReadDWord(stream);
						stream.Read(buff, 16 * 16 * 16 * y[0], length);
					}

					if (!ret)
						SkipType(stream, type[0]);
				}

				nsections--;
			}
			return true;
		}


		private int ReadWord(MemoryStream stream)
		{
			byte[] buf = new byte[2];
			stream.Read(buf, 0, 2);
			return ((buf[0] << 8) | buf[1]);
		}

		private int ReadDWord(MemoryStream stream)
		{
			byte[] buf = new byte[4];
			stream.Read(buf, 0, 4);
			return ((buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3]);
		}

		private int FindElement(MemoryStream stream, string name)
		{
			while (true)
			{
				byte[] type = new byte[1];
				stream.Read(type, 0, 1);
				if (type[0] == 0)
					return 0;

				if (Compare(stream, name))
					return type[0];
				SkipType(stream, type[0]);
			}
		}


		private void SkipType(MemoryStream stream, byte type)
		{
			int length;
			switch (type)
			{
				case 1: //byte
					stream.Seek(1, SeekOrigin.Current);
					break;
				case 2: // short
					stream.Seek(2, SeekOrigin.Current);
					break;
				case 3: // int
					stream.Seek(4, SeekOrigin.Current);
					break;
				case 4: // long
					stream.Seek(8, SeekOrigin.Current);
					break;
				case 5: //float
					stream.Seek(4, SeekOrigin.Current);
					break;
				case 6: //double
					stream.Seek(8, SeekOrigin.Current);
					break;
				case 7: // byte array
					length = ReadDWord(stream);
					stream.Seek(length, SeekOrigin.Current);
					break;
				case 8://string
					length = ReadWord(stream);
					stream.Seek(length, SeekOrigin.Current);
					break;
				case 9: //list
					SkipList(stream);
					break;
				case 10: //Compound
					SkipCompound(stream);
					break;
				case 11: // int Array
					length = ReadDWord(stream);
					stream.Seek(length * 4, SeekOrigin.Current);
					break;
			}
		}


		private void SkipList(MemoryStream stream)
		{
			int length;
			byte[] type = new byte[1];
			stream.Read(type, 0, 1);
			length = ReadDWord(stream);
			switch (type[0])
			{
				case 1: //byte
					stream.Seek(length, SeekOrigin.Current);
					break;
				case 2://short
					stream.Seek(length * 2, SeekOrigin.Current);
					break;
				case 3://int
					stream.Seek(length * 4, SeekOrigin.Current);
					break;
				case 4: //long
					stream.Seek(length * 8, SeekOrigin.Current);
					break;
				case 5: //float
					stream.Seek(length * 4, SeekOrigin.Current);
					break;
				case 6: //double
					stream.Seek(length * 8, SeekOrigin.Current);
					break;
				case 7: // byte aray
					for (int i = 0; i < length; i++)
					{
						int slength = ReadDWord(stream);
						stream.Seek(slength, SeekOrigin.Current);
					}
					break;
				case 8://string
					for (int i = 0; i < length; i++)
					{
						int slength = ReadWord(stream);
						stream.Seek(slength, SeekOrigin.Current);
					}
					break;
				case 9: //List
					for (int i = 0; i < length; i++)
					{
						SkipList(stream);
					}
					break;
				case 10: //compound
					for (int i = 0; i < length; i++)
					{
						SkipCompound(stream);
					}
					break;
				case 11: //int array
					for (int i = 0; i < length; i++)
					{
						int slength = ReadDWord(stream);
						stream.Seek(slength * 4, SeekOrigin.Current);
					}
					break;
			}
		}

		private void SkipCompound(MemoryStream stream)
		{
			int length;
			byte[] type = new byte[1];
			do
			{
				stream.Read(type, 0, 1);
				if (type[0] != 0)
				{
					length = ReadWord(stream);
					stream.Seek(length, SeekOrigin.Current);
					SkipType(stream, type[0]);
				}
			} while (type[0] != 0);
		}

		private bool Compare(MemoryStream stream, string name)
		{
			int length = ReadWord(stream);

			byte[] nameB = new byte[length + 1];
			stream.Read(nameB, 0, length);
			nameB[length] = 0;
			char[] temp = new char[length + 1];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i] = (char)nameB[i];
			}

			string thisName = new string(temp);
			if (string.Compare(thisName, name) == 0)
				return true;

			return false;
		}
	}
}
