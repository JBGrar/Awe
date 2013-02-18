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

		private bool GetBlocks(MemoryStream memoryStream, byte[] blockData)
		{
			throw new NotImplementedException();
		}
	}
}
