using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2Server
{
    internal class RandomMT
    {
        public uint[] state;

        public uint f;

        public uint m;

        public uint u;

        public uint s;

        public uint b;

        public uint t;

        public uint c;

        public uint l;

        public uint index;

        public uint lower_mask;

        public uint upper_mask;

        public RandomMT(uint seed)
        {
            this.state = new uint[624];
            this.f = 1812433253U;
            this.m = 397U;
            this.u = 11U;
            this.s = 7U;
            this.b = 2636928640U;
            this.t = 15U;
            this.c = 4022730752U;
            this.l = 18U;
            this.index = 624U;
            this.lower_mask = 2147483647U;
            this.upper_mask = 2147483648U;
            this.state[0] = seed;

            for (int i = 1; i < 624; i++)
            {
                this.state[i] = Convert.ToUInt32((long)((ulong)(this.f * (this.state[i - 1] ^ (this.state[i - 1] >> 30))) + (ulong)((long)i)));
            }
        }
        public void ReinitState()
        {
            for (uint num = 0U; num < 624U; num += 1U)
            {
                uint num2 = Convert.ToUInt32((long)((ulong)((this.state[(int)num] & this.upper_mask) + (this.state[(int)((num + 1U) % 624U)] & this.lower_mask))));
                uint num3 = num2 >> 1;
                bool flag = num2 % 2U > 0U;
                if (flag)
                {
                    num3 = Convert.ToUInt32((long)((ulong)(num3 ^ 2567483615U)));
                }
                this.state[(int)num] = this.state[(int)((num + this.m) % 624U)] ^ num3;
            }
            this.index = 0U;
        }
        public uint Rand()
        {
            if (this.index >= state.Length)
            {
                this.ReinitState();
            }
            uint num = this.state[(int)this.index];
            num ^= num >> (int)this.u;
            num ^= (num << (int)this.s) & this.b;
            num ^= (num << (int)this.t) & this.c;
            num ^= num >> (int)this.l;
            this.index += 1U;
            return Convert.ToUInt32((long)((ulong)num));

        }

        public int NextInt32(int mn, int mx)
        {
            uint rand = Rand();
            return (int)((long)mn + (long)((ulong)rand % (ulong)((long)(mx-mn))));
        }

    }
}
