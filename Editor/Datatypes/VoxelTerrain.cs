using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AweEditor
{
    /// <summary>
    /// BlockType corresponds to Minecraft Bock IDs for ease of import
    /// (see http://www.minecraftwiki.net/wiki/Data_values#Block_IDs)
    /// </summary>
    public enum BlockType
    {
        Air = 0,
        Stone = 1,
        // TODO: Implement remaining block types
    }

    /// <summary>
    /// A class to represent voxel terrain
    /// </summary>
    public class VoxelTerrain
    {
		private VoxelChunk[,] _chunks;

		public VoxelChunk[,] Chunks
		{
			get
			{
				return _chunks;
			}
		}

		public VoxelTerrain()
		{
			_chunks = new VoxelChunk[32, 32];
		}

		public VoxelChunk GetChunk(int x, int z)
		{
			return _chunks[x, z];
		}

		public BlockType GetBlock(int cx, int cz, int x, int y, int z)
		{
			return _chunks[cx, cz].GetBlock(x, y, z);
		}

		public BlockType[,] GetXZPlane(int y)
		{
			BlockType[,] plane = new BlockType[32*16,32*16];

			for (int cx = 0; cx < 32; cx++)
			{
				for (int cz = 0; cz < 32; cz++)
				{
					BlockType[,] temp = _chunks[cx, cz].GetXZSlice(y);
					for (int x = 0; x < 16; x++)
					{
						for (int z = 0; z < 16; z++)
						{
							plane[cx*16+x, cz + z] = temp[x, z];
						}
					}
				}
			}
			return plane;
		}

		public BlockType[,] GetXYPlaine(int z)
		{
			BlockType[,] plain = new BlockType[16*32,256]; 
			for (int cx = 0; cx < 32; cx++)
			{
				BlockType[,] temp = new BlockType[16, 256];
				for (int x = 0; x < 16; x++)
				{
					for (int y = 0; y < 256; y++)
					{
						plain[x + cx*16, y] = temp[x, y];
					}
				}
			}
			return null;
		}


        // TODO: Complete class
    }

	public class VoxelChunk
	{
		private BlockType[, ,] _blocks;

		public BlockType[, ,] Blocks
		{
			get
			{
				return _blocks;
			}
		}
		public VoxelChunk()
		{
			_blocks = new BlockType[256, 16, 16];
		}

		public VoxelChunk(BlockType[] blockData)
		{
			_blocks = new BlockType[256, 16, 16];
			for (int y = 0; y < 256; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					for (int x = 0; x < 16; x++)
					{
						_blocks[y,z,x] = blockData[(y*16*16)+(z*16)+x];
					}
				}
			}
		}

		public BlockType GetBlock(int x, int y, int z)
		{

			return _blocks[y, z, x];
		}

		public BlockType[,] GetYZSlice(int x)
		{
			BlockType[,] slice = new BlockType[256, 16];
			for (int y = 0; y < 256; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					slice[y, z] = Blocks[y, z, x];
				}
			}
			return slice;
		}

		public BlockType[,] GetYXSlice(int z)
		{
			BlockType[,] slice = new BlockType[256, 16];
			for (int y = 0; y < 256; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					slice[y, x] = Blocks[y, z, x];
				}
			}
			return slice;
		}

		public BlockType[,] GetXZSlice(int y)
		{
			BlockType[,] slice = new BlockType[16, 16];
			for (int z = 0; z < 16; z++)
			{
				for (int x = 0; x < 16; x++)
				{
					slice[x, z] = _blocks[y, z, x];
				}
			}
			return slice;
		}

		public BlockType[] GetZLine(int x, int y)
		{
			BlockType[] line = new BlockType[16];
			for (int z = 0; z < 16; z++)
			{
				line[z] = _blocks[y, z, x];
			}
			return line;
		}

		public BlockType[] GetXLine(int y, int z)
		{
			BlockType[] line = new BlockType[16];
			for (int x = 0; x < 16; x++)
			{
				line[x] = _blocks[y, z, x];
			}
			return line;
		}

		public BlockType[] GetYLine(int x, int z)
		{
			BlockType[] line = new BlockType[256];
			for (int y = 0; y < 256; y++)
			{
				line[y] = _blocks[y, z, x];
			}
			return line;
		}
	}
}
