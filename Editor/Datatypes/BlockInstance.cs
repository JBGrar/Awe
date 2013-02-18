using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AweEditor
{
	class BlockInstance
	{
		int size,x,y,z;
		BlockType type;

		public BlockInstance(int x, int y, int z, BlockType type)
		{
			size = 1;
			this.x = x;
			this.y = y;
			this.z = z;
			this.type = type;
		}

		public Matrix Transform
		{
			get
			{
				return transform;
			}
		}

		Matrix transform;
	}
}
