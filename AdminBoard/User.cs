using System.Collections;
using System.Collections.Generic;


namespace AdminBoard
{
    public class User
    {
        /*Patametrs*/
        public int group { get; internal set; }
        public string uId { get; internal set; }
        public string name { get; internal set; }
        public int Health { get; internal set; }
        public int SolderClass { get; internal set; }
        /*Transform*/
        public Vector2 rotation { get; internal set; }
        public Vector3 position { get; internal set; }
        /*Hidden prop*/
        List<int> DamageLog;

        public User(string name, string uId)
        {
            this.name = name;
            this.uId = uId;
            Health = 100;
            position = new Vector3();
            rotation = new Vector2();
            DamageLog = new List<int>();
            group = 0;
        }
        public void setDamage(int damage)
        {
            Health -= damage;
            DamageLog.Add(damage);
        }
        public void setPosition(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
        }
        public void setRotation(float x, float y)
        {
            rotation = new Vector2(x, y);
        }
        /*
         * 0 - no group
         * 
         * 1 - blue
         * 2 - red
         */
        public void setCommand(int group)
        {
            this.group = group;
        }
        
    }
}
