using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Missile
    {
        public Vector3 _pos;
        public Vector3 _rot;

        public void Update()
        {


        }
    }

    public class MissileManager
    {
        public static MissileManager Instance { get; } = new MissileManager();
        public Dictionary<int, Missile> Missiles = new Dictionary<int, Missile>();


        public void Add()
        {

        }
    }
}
