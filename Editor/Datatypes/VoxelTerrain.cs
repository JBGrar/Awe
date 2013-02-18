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
						_blocks[y,z,x] = blockData[(y*16*16)+(z*16)+x)];
					}
				}
			}
		}

		public BlockType GetBlock(int x, int y, int z)
		{
			return _blocks[y, z, x];
		}
	}
}
