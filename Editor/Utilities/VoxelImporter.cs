using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AweEditor.Utilities
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

				input = new byte[CHUNK_DEFAULT_MAX];
				output = new byte[CHUNK_DEFAULT_MAX];
				fs.Seek(4 * ((cx & 31) + (cz & 31) * 32), SeekOrigin.Begin);
				fs.Read(buff, 0, 4);

				sectorNumber = buff[3];
				offset = (buff[0] << 16) | (buff[1] << 8) | buff[2];

				if (offset == 0)
					return false;

				fs.Seek(4096 * offset, SeekOrigin.Begin);

				fs.Read(buff, 0, 5);

				chunkLength = (buff[0]<<24)|(buff[1]<<16)|(buff[2]<<8)|buff[3];
				
				if((chunkLength>sectorNumber*4096)||(chunkLength>CHUNK_DEFAULT_MAX)
					return false;

				if(fs.Read(input,0,chunkLength-1)==0)
					return false;

				DeflateStream dfs = new DeflateStream(new MemoryStream(input),
													CompressionMode.Decompress);
				dfs.Flush();
				dfs.Read(output,0,input.Length);
				return GetBlocks(new MemoryStream(output),blockData);
			}
			
		}

		private bool GetBlocks(MemoryStream bf, byte[] buff)
		{
			int length, found, nsections;

			bf.Seek(1, SeekOrigin.Current);
			length = ReadWord(bf);
			bf.Seek(length, SeekOrigin.Current);

			if (FindElement(bf, "Level") != 10)
				return false;

			if(FindElement(bf, "Biomes")!=7)
				return false;

			//skip biome data
			bf.Seek(4,SeekOrigin.Current);
			bf.Seek(16*16,SeekOrigin.Current);

			if (FindElement(bf, "Sections") != 9)
				return false;
			{
				byte[] type = new byte[1];
				bf.Read(type, 0, 1);

				if (type[0] != 10)
					return false;
			}

			nsections = ReadDWord(bf);
			while ((nsections) > 0)
			{
				byte[] y = new byte[1];
				long save = bf.Position;

				if (FindElement(bf, "Y") != 1)
					return false;

				bf.Read(y, 0, 1);
				bf.Seek(save, SeekOrigin.Begin);

				found = 0;

				while (true)
				{
					bool ret = false;
					byte[] type = new byte[1];
					bf.Read(type, 0, 1);
					if (type[0] == 0)
						break;
					length = ReadWord(bf);

					byte[] name = new byte[length + 1];
					bf.Read(name, 0, length);
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
						length = ReadDWord(bf);
						bf.Seek(length, SeekOrigin.Current);
					}
					if (string.Compare(thisName, "Blocks") == 0)
					{
						found++;
						ret = true;
						length = ReadDWord(bf);
						bf.Read(buff, 16 * 16 * 16 * y[0], length);
					}

					if (!ret)
						SkipType(bf, type[0]);
				}

				nsections--;
			}
			return true;
		}


		private int ReadWord(MemoryStream bf)
		{
			byte[] buf = new byte[2];
			bf.Read(buf, 0, 2);
			return ((buf[0] << 8) | buf[1]);
		}

		private int ReadDWord(MemoryStream bf)
		{
			byte[] buf = new byte[4];
			bf.Read(buf, 0, 4);
			return ((buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3]);
		}

		private int FindElement(MemoryStream bf, string name)
		{
			while (true)
			{
				byte[] type = new byte[1];
				bf.Read(type, 0, 1);
				if (type[0] == 0)
					return 0;

				if (Compare(bf, name))
					return type[0];
				SkipType(bf, type[0]);
			}
		}


		private void SkipType(MemoryStream bf, byte type)
		{
			int length;
			switch (type)
			{
				case 1: //byte
					bf.Seek(1, SeekOrigin.Current);
					break;
				case 2: // short
					bf.Seek(2, SeekOrigin.Current);
					break;
				case 3: // int
					bf.Seek(4, SeekOrigin.Current);
					break;
				case 4: // long
					bf.Seek(8, SeekOrigin.Current);
					break;
				case 5: //float
					bf.Seek(4, SeekOrigin.Current);
					break;
				case 6: //double
					bf.Seek(8, SeekOrigin.Current);
					break;
				case 7: // byte array
					length = ReadDWord(bf);
					bf.Seek(length, SeekOrigin.Current);
					break;
				case 8://string
					length = ReadWord(bf);
					bf.Seek(length, SeekOrigin.Current);
					break;
				case 9: //list
					SkipList(bf);
					break;
				case 10: //Compound
					SkipCompound(bf);
					break;
				case 11: // int Array
					length = ReadDWord(bf);
					bf.Seek(length * 4, SeekOrigin.Current);
					break;
			}
		}


		private void SkipList(MemoryStream bf)
		{
			int length;
			byte[] type = new byte[1];
			bf.Read(type, 0, 1);
			length = ReadDWord(bf);
			switch (type[0])
			{
				case 1: //byte
					bf.Seek(length, SeekOrigin.Current);
					break;
				case 2://short
					bf.Seek(length * 2, SeekOrigin.Current);
					break;
				case 3://int
					bf.Seek(length * 4, SeekOrigin.Current);
					break;
				case 4: //long
					bf.Seek(length * 8, SeekOrigin.Current);
					break;
				case 5: //float
					bf.Seek(length * 4, SeekOrigin.Current);
					break;
				case 6: //double
					bf.Seek(length * 8, SeekOrigin.Current);
					break;
				case 7: // byte aray
					for (int i = 0; i < length; i++)
					{
						int slength = ReadDWord(bf);
						bf.Seek(slength, SeekOrigin.Current);
					}
					break;
				case 8://string
					for (int i = 0; i < length; i++)
					{
						int slength = ReadWord(bf);
						bf.Seek(slength, SeekOrigin.Current);
					}
					break;
				case 9: //List
					for (int i = 0; i < length; i++)
					{
						SkipList(bf);
					}
					break;
				case 10: //compound
					for (int i = 0; i < length; i++)
					{
						SkipCompound(bf);
					}
					break;
				case 11: //int array
					for (int i = 0; i < length; i++)
					{
						int slength = ReadDWord(bf);
						bf.Seek(slength * 4, SeekOrigin.Current);
					}
					break;
			}
		}

		private void SkipCompound(MemoryStream bf)
		{
			int length;
			byte[] type = new byte[1];
			do
			{
				bf.Read(type, 0, 1);
				if (type[0] != 0)
				{
					length = ReadWord(bf);
					bf.Seek(length, SeekOrigin.Current);
					SkipType(bf, type[0]);
				}
			} while (type[0] != 0);
		}

		private bool Compare(MemoryStream bf, string name)
		{
			int length = ReadWord(bf);

			byte[] nameB = new byte[length + 1];
			bf.Read(nameB, 0, length);
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
