using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace AweEditor
{
	class VoxelImporter
	{
		const int CHUNK_DEFLATE_MAX = 1024 * 64;
		const int CHUNK_INFLATE_MAX = 1024 * 128;
		int initialOffset;
		public VoxelTerrain ImportRegion(string fileName)
		{
			byte[] fileBuffer = File.ReadAllBytes(fileName);
			BlockType[] blocks;
			VoxelTerrain vt = new VoxelTerrain();
			for (int x = 0; x < 32; x++)
			{
				for (int z = 0; z < 32; z++)
				{
					blocks = new BlockType[65536];
					if(GetChunk(fileBuffer, blocks, x, z))
						vt.AddChunk(x, z, blocks);
				}
			}
			return vt;
		}

		private bool GetChunk(byte[] buff, BlockType[] blocks,int cx,int cz)
		{
			int position;
			int sectorNumber, offset, chunkLength;
			byte[] buf = new byte[5];
			byte[] input; //= new byte[CHUNK_DEFLATE_MAX];
			byte[] output;// = new byte[CHUNK_INFLATE_MAX];

			position = 4*((cx&31)+(cz&31)*32);

			for (int i = 0; i < 4; i++)
			{
				buf[i] = buff[position + i];
			}
			position += 4;

			sectorNumber = buf[3];
			offset = (buf[0] << 16) | (buf[1] << 8) | buf[2];
			
			if (offset == 0)
				return false;

			position = 4096 * offset;

			for(int i =0;i<5;i++)
			{
				buf[i] = buff[position+i];
			}
			position += 5;

			chunkLength=(buf[0]<<24)|(buf[1]<<16)|(buf[2]<<8)|buf[3];

			if((chunkLength>sectorNumber*4096)||(chunkLength>CHUNK_DEFLATE_MAX))
				return false;
			input = new byte[CHUNK_DEFLATE_MAX];
			Array.Copy(buff,position+2,input,0,chunkLength-6);
			output = new byte[CHUNK_INFLATE_MAX];
			DeflateStream dfs = new DeflateStream(new MemoryStream(input), CompressionMode.Decompress);
			dfs.Flush();
			dfs.Read(output,0,input.Length);
			

			return GetBlocks(output,blocks);
		}

		/// <summary>
		/// Takes a file buffer and modifies the blocks array
		/// </summary>
		/// <param name="buff"></param>
		/// <param name="blocks"></param>
		/// <returns></returns>
		private bool GetBlocks(byte[] buff, BlockType[] blocks)
		{
			int length, found, nsections, position;

			position = 0;
			length = ReadWord(buff, position);
			position += length;

			if (FindTagElement(buff, position, "Level") != 10)
				return false;

			if (FindTagElement(buff, position, "Biomes") != 7)
				return false;

			if (FindTagElement(buff, position, "Sections") != 9)
				return false;
			//may be missing a type check

			//reset blocks;

			nsections = ReadDWord(buff, position);

			while (nsections > 0)
			{
				byte y;
				if (FindTagElement(buff, position, "Y") != 1)
					return false;

				y = buff[position];
				position++;
				//SEEK_SET?
				found = 0;
				while (true)
				{
					bool ret = false;
					byte type = 0;
					type = buff[position];
					position++;
					if (type == 0)
						break;

					length = ReadWord(buff, position);
					string curName = GetStringFromBytes(buff, position, length);

					if (string.Compare(curName, "Blocks") == 0)
					{
						found++;
						ret = true;
						length = ReadDWord(buff, position);
						ReadChunkSection(buff, position, blocks, 16 * 16 * 16 * y, length);
					}

					if (!ret)
						SkipType(buff, position, type);
				}

				nsections--;
			}
			return true;
		}

		private void ReadChunkSection(byte[] buff, int position, BlockType[] blocks, int blocksPosition, int length)
		{
			for (int i = 0; i < length; i++)
			{
				blocks[blocksPosition + i] = (BlockType)buff[position + i];
			}
			position += length;
		}

		private void SkipType(byte[] buff, int position, byte type)
		{
			int length;
			switch (type)
			{
				case 1: //byte
					position++;
					break;
				case 2: //short
					position += 2;
					break;
				case 3: //int
					position += 4;
					break;
				case 4: //Long
					position += 8;
					break;
				case 5: //float
					position += 4;
					break;
				case 6: //double
					position += 8;
					break;
				case 7: //byte array
					length = ReadDWord(buff, position);
					position += length;
					break;
				case 8: //string
					length = ReadWord(buff, position);
					position += length;
					break;
				case 9: //list
					SkipList(buff, position);
					break;
				case 10: //compound
					SkipCompound(buff, position);
					break;
				case 11: //int array
					length = ReadDWord(buff, position);
					position += length * 4;
					break;
				default:
					throw new Exception("Unkown Tag Type");

			}
		}

		private void SkipCompound(byte[] buff, int position)
		{
			int length;
			byte type = 0;
			do
			{
				type = buff[position];
				position++;
				if (type != 0)
				{
					length = ReadWord(buff, position);
					position += length;
					SkipType(buff, position, type);
				}
			} while (type != 0);
		}

		private void SkipList(byte[] buff, int position)
		{
			int length;
			int temp;
			byte type;
			type = buff[position];
			position++;
			length = ReadDWord(buff, position);
			switch (type)
			{
				case 1: //byte
					position += length;
					break;
				case 2: //short
					position += length * 2;
					break;
				case 3: //int
					position += length * 4;
					break;
				case 4: //long
					position += length * 8;
					break;
				case 5: //float
					position += length * 4;
					break;
				case 6: //double
					position += length * 8;
					break;
				case 7: // byte array
					for (int i = 0; i < length; i++)
					{
						temp = ReadDWord(buff, position);
						position += temp;
					}
					break;
				case 8:  //String;
					for (int i = 0; i < length; i++)
					{
						temp = ReadWord(buff, position);
						position += temp;
					}
					break;
				case 9: // list
					for (int i = 0; i < length; i++)
					{
						SkipList(buff, position);
					}
					break;
				case 10: //Compound
					for (int i = 0; i < length; i++)
					{
						SkipCompound(buff, position);
					}
					break;
				case 11: // int array
					for (int i = 0; i < length; i++)
					{
						temp = ReadDWord(buff, position);
						position += temp * 4;
					}
					break;
				default:
					throw new Exception("Unkown Type Encountered in SkipList()");
			}

		}

		/// <summary>
		/// Gets a string from the input buffer using the given length and current position.
		/// </summary>
		/// <param name="buff">the input buffer</param>
		/// <param name="position">current Position in the buffer</param>
		/// <param name="length">number of bytes in the string</param>
		/// <returns>a string constructed from the input buffer of the given length</returns>
		private string GetStringFromBytes(byte[] buff, int position, int length)
		{
			char[] temp = new char[length];
			for (int i = 0; i < length; i++)
			{
				temp[i] = (char)buff[position + i];
			}
			position += length;

			return new string(temp);
		}

		/// <summary>
		/// Reads a word from the given byte buffer and puts it in the proper order
		/// </summary>
		/// <param name="buff">given buffer</param>
		/// <param name="position">current position in the buffer</param>
		/// <returns>an ordered double word</returns>
		private int ReadDWord(byte[] buff, int position)
		{
			byte[] temp = new byte[4];
			for (int i = 0; i < 4; i++)
			{
				temp[i] = buff[position];
				position++;
			}
			return (int)((temp[0] << 24) | (temp[1] << 16) | (temp[2] << 8) | temp[3]);
		}

		/// <summary>
		/// Reads a word from the given byte buffer and puts it in the proper order
		/// </summary>
		/// <param name="buff">given buffer</param>
		/// <param name="position">current position in the buffer</param>
		/// <returns>an ordered word</returns>
		private short ReadWord(byte[] buff, int position)
		{
			byte[] temp = new byte[2];
			for (int i = 0; i < 2; i++)
			{
				temp[i] = buff[position];
				position++;
			}
			return (short)((temp[0] << 8) | temp[1]);
		}

		private int FindTagElement(byte[] buff, int position, string name)
		{
			byte type;
			while (true)
			{
				if (position > buff.Length)
					return 0;
				type = 0;
				type = buff[position];
				position++;
				if (type == 0)
					return 0;

				if (Compare(buff, position, name))
					return type;
				SkipType(buff, position, type);
			}
		}

		private bool Compare(byte[] buff, int position, string name)
		{
			int length = ReadWord(buff, position);
			string temp = GetStringFromBytes(buff, position, length);
			if (string.Compare(temp, name) == 0)
				return true;

			return false;
		}

	}
}
