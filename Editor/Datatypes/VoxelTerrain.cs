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
		Grass = 2,
		Dirt = 3,
		Cobblestone = 4,
		WoodenPlank = 5,
		Sapling = 6,
		Bedrock = 7,
		Water = 8,
		StationaryWater = 9,
		Lava = 10,
		StationaryLava = 11,
		Sand =12,
		Gravel = 13,
		GoldOre=14,
		IronOre=15,
		CoalOre=16,
		Wood=17,
		Leaves=18,
		Sponge=19,
		Glass=20,
		LapisLazuliOre=21,
		LapisLazuliBlock=22,
		sandstone=24,
		Wool=35,
		BlockOfGold=41,
		BlockOfIron=42,
		DoubleSLabs=43,
		Slabs = 44,
		Bricks = 45,
		TNT = 46,
		BookShelf = 47,
		MossStone = 48,
		Obsidian=49,
		DiamondOre=56,
		BlockOfDiamond=57,
		RedstoneOre=73,
		Ice=79,
		SnowBlock=80,
		Cactus=81,
		ClayBlock=82,
		Pumpkin=86,
		Netherrack=87,
		SoulSand=88,
		GlowstoneBlock=89,
		Mycelium = 110,
		NetherBrick=112,

        // TODO: Implement remaining block types
    }

    /// <summary>
    /// A class to represent voxel terrain
    /// </summary>
    public class VoxelTerrain
    {
		private VoxelChunk[,] chunks = new VoxelChunk[32, 32];

		public VoxelChunk GetChunk(int z, int x)
		{
			return chunks[z, x];
		}

		public BlockType GetBlockInChunk(int cz, int cx, int x, int y, int z)
		{
			return chunks[cz, cx].GetBlock(x, y, z);
		}

		public void AddChunk(int cx, int cz, BlockType[] blocks)
		{
			chunks[cz, cx] = new VoxelChunk(blocks);
		}
    }

	public class VoxelChunk
	{
		private BlockType[, ,] blocks;
		private bool _isEmptyChunk;

		public bool IsEmpty
		{
			get
			{
				return _isEmptyChunk;
			}
		}

		public BlockType[, ,] AllBlocks
		{
			get
			{
				return blocks;
			}
		}

		public BlockType GetBlock(int x, int y, int z)
		{
			if (_isEmptyChunk)
			{
				return BlockType.Air;
			}
			return blocks[y, z, x];
		}

		public VoxelChunk()
		{
			_isEmptyChunk = true;
		}

		public VoxelChunk(BlockType[] blockData)
		{
			_isEmptyChunk = false;
			blocks = new BlockType[256, 16, 16];
			for (int y = 0; y < 256; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					for (int x = 0; x < 16; x++)
					{
						blocks[y, z, x] = blockData[(y * 16 * 16 + z * 16 + x)];
					}
				}
			}
		}
	}
}
